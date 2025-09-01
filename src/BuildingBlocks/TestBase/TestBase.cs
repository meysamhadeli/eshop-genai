using System.Net;
using System.Security.Claims;
using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using BuildingBlocks.EFCore;
using BuildingBlocks.MassTransit;
using BuildingBlocks.Mongo;
using BuildingBlocks.Web;
using Duende.IdentityServer.Models;
using Grpc.Net.Client;
using MassTransit.Testing;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Npgsql;
using NSubstitute;
using Respawn;
using Testcontainers.EventStoreDb;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WebMotions.Fake.Authentication.JwtBearer;
using Xunit;
using Xunit.Abstractions;

namespace BuildingBlocks.TestBase;


public class TestFixture<TEntryPoint> : IAsyncLifetime
where TEntryPoint : class
{
    private readonly WebApplicationFactory<TEntryPoint> _factory;
    private int Timeout => 120; // Second
    private ITestHarness TestHarness => ServiceProvider?.GetTestHarness();
    private Action<IServiceCollection> TestRegistrationServices { get; set; }
    private PostgreSqlContainer PostgresTestcontainer;
    private PostgreSqlContainer PostgresOutboxTestContainer;
    public RabbitMqContainer RabbitMqTestContainer;
    public MongoDbContainer MongoDbTestContainer;
    public EventStoreDbContainer EventStoreDbTestContainer;
    public CancellationTokenSource CancellationTokenSource;

    public ITestHarness MassTransitTestHarness => ServiceProvider.GetTestHarness();

    public HttpClient HttpClient
    {
        get
        {
            var claims = new Dictionary<string, object>
                         {
                             { ClaimTypes.Name, "test@sample.com" },
                             { ClaimTypes.Role, "admin" },
                             { "scope", "flight-api" }
                         };

            var httpClient = _factory.CreateClient();
            httpClient.SetFakeBearerToken(claims);
            return httpClient;
        }
    }

    public GrpcChannel Channel =>
        GrpcChannel.ForAddress(
            HttpClient.BaseAddress!,
            new GrpcChannelOptions { HttpClient = HttpClient });

    public IServiceProvider ServiceProvider => _factory?.Services;
    public IConfiguration Configuration => _factory?.Services.GetRequiredService<IConfiguration>();
    public ILogger Logger { get; set; }

    protected TestFixture()
    {
        _factory = new WebApplicationFactory<TEntryPoint>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureAppConfiguration(AddCustomAppSettings);
                    builder.UseEnvironment("test");

                    builder.ConfigureServices(
                        services =>
                        {
                            TestRegistrationServices?.Invoke(services);
                            services.ReplaceSingleton(AddHttpContextAccessorMock);

                            // Register all ITestDataSeeder implementations
                            services.Scan(scan => scan
                                              .FromApplicationDependencies()
                                              .AddClasses(classes => classes.AssignableTo<ITestDataSeeder>())
                                              .AsImplementedInterfaces()
                                              .WithScopedLifetime());

                            // Add Fake JWT Authentication
                            services.AddAuthentication(
                                    options =>
                                    {
                                        options.DefaultAuthenticateScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                                        options.DefaultChallengeScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                                    })
                                .AddFakeJwtBearer();

                            // Mock Authorization Policies
                            services.AddAuthorization(options =>
                                   {
                                       options.AddPolicy(nameof(ApiScope), policy =>
                                       {
                                           policy.AddAuthenticationSchemes(FakeJwtBearerDefaults.AuthenticationScheme);
                                           policy.RequireAuthenticatedUser();
                                           policy.RequireClaim("scope", "catalog-api");
                                       });
                                   });

                            // Configure faster outbox for tests
                            services.Configure<PostgresOutboxOptions>(options =>
                            {
                                options.QueryDelay = TimeSpan.FromMilliseconds(100);
                                options.QueryTimeout = TimeSpan.FromSeconds(1);
                                options.MessageDeliveryLimit = 10;
                            });
                        });
                });
    }

    public async Task InitializeAsync()
    {
        CancellationTokenSource = new CancellationTokenSource();
        await StartTestContainerAsync();
    }

    public async Task DisposeAsync()
    {
        await StopTestContainerAsync();
        await _factory.DisposeAsync();
        await CancellationTokenSource.CancelAsync();
    }

    public virtual void RegisterServices(Action<IServiceCollection> services)
    {
        TestRegistrationServices += services;
    }

    public ILogger CreateLogger(ITestOutputHelper output)
    {
        if (output == null)
            return null;

        var loggerFactory = LoggerFactory.Create(builder =>
                                                 {
                                                     builder.AddXunit(output);
                                                     builder.SetMinimumLevel(LogLevel.Debug);
                                                 });
        return loggerFactory.CreateLogger("TestLogger");
    }

    protected async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        await action(scope.ServiceProvider);
    }

    protected async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = ServiceProvider.CreateScope();
        var result = await action(scope.ServiceProvider);
        return result;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        return ExecuteScopeAsync(
            sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                return mediator.Send(request);
            });
    }

    public Task SendAsync(IRequest request)
    {
        return ExecuteScopeAsync(
            sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                return mediator.Send(request);
            });
    }

    public async Task Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IEvent
    {
        await TestHarness.Bus.Publish(message, cancellationToken);
    }

    public async Task<bool> WaitForPublishing<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class, IEvent
    {
        return await WaitUntilConditionMet(
            async () => await TestHarness.Published.Any<TMessage>(cancellationToken));
    }

    public async Task<bool> WaitForConsuming<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class, IEvent
    {
        return await WaitUntilConditionMet(
            async () => await TestHarness.Consumed.Any<TMessage>(cancellationToken));
    }

    public async Task<bool> WaitForOutboxMessageProcessed<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return await WaitUntilConditionMet(
            async () =>
            {
                var sent = await TestHarness.Sent.Any<TMessage>(cancellationToken);
                var published = await TestHarness.Published.Any<TMessage>(cancellationToken);
                return sent || published;
            });
    }

    private async Task<bool> WaitUntilConditionMet(Func<Task<bool>> conditionToMet, int? timeoutSecond = null)
    {
        var time = timeoutSecond ?? Timeout;
        var startTime = DateTime.Now;
        var timeoutExpired = false;
        var meet = await conditionToMet.Invoke();

        while (!meet && !timeoutExpired)
        {
            await Task.Delay(100);
            meet = await conditionToMet.Invoke();
            timeoutExpired = DateTime.Now - startTime > TimeSpan.FromSeconds(time);
        }

        return meet;
    }

    private async Task StartTestContainerAsync()
    {
        PostgresTestcontainer = TestContainers.PostgresTestContainer();
        PostgresOutboxTestContainer = TestContainers.PostgresOutboxTestContainer();
        RabbitMqTestContainer = TestContainers.RabbitMqTestContainer();
        MongoDbTestContainer = TestContainers.MongoTestContainer();
        EventStoreDbTestContainer = TestContainers.EventStoreTestContainer();

        await MongoDbTestContainer.StartAsync();
        await PostgresTestcontainer.StartAsync();
        await PostgresOutboxTestContainer.StartAsync();
        await RabbitMqTestContainer.StartAsync();
        await EventStoreDbTestContainer.StartAsync();
    }

    private async Task StopTestContainerAsync()
    {
        await PostgresTestcontainer.StopAsync();
        await PostgresOutboxTestContainer.StopAsync();
        await RabbitMqTestContainer.StopAsync();
        await MongoDbTestContainer.StopAsync();
        await EventStoreDbTestContainer.StopAsync();
    }

    private void AddCustomAppSettings(IConfigurationBuilder configuration)
    {
        configuration.AddInMemoryCollection(
            new KeyValuePair<string, string>[]
            {
                new("PostgresOptions:ConnectionString", PostgresTestcontainer.GetConnectionString()),
                new("PostgresOptions:ConnectionString:Flight", PostgresTestcontainer.GetConnectionString()),
                new("PostgresOptions:ConnectionString:Identity", PostgresTestcontainer.GetConnectionString()),
                new("PostgresOptions:ConnectionString:Passenger", PostgresTestcontainer.GetConnectionString()),
                new("PostgresOutboxOptions:ConnectionString", PostgresOutboxTestContainer.GetConnectionString()),
                new("RabbitMqOptions:HostName", RabbitMqTestContainer.Hostname),
                new("RabbitMqOptions:UserName", TestContainers.RabbitMqContainerConfiguration.UserName),
                new("RabbitMqOptions:Password", TestContainers.RabbitMqContainerConfiguration.Password),
                new("RabbitMqOptions:Port", RabbitMqTestContainer.GetMappedPublicPort(5672).ToString()),
                new("MongoOptions:ConnectionString", MongoDbTestContainer.GetConnectionString()),
                new("MongoOptions:DatabaseName", TestContainers.MongoContainerConfiguration.Name),
                new("EventStoreOptions:ConnectionString", EventStoreDbTestContainer.GetConnectionString())
            });
    }

    private IHttpContextAccessor AddHttpContextAccessorMock(IServiceProvider serviceProvider)
    {
        var httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();
        using var scope = serviceProvider.CreateScope();

        httpContextAccessorMock.HttpContext = new DefaultHttpContext
        { RequestServices = scope.ServiceProvider };

        httpContextAccessorMock.HttpContext.Request.Host = new HostString("localhost", 6012);
        httpContextAccessorMock.HttpContext.Request.Scheme = "http";

        return httpContextAccessorMock;
    }
}

public class TestWriteFixture<TEntryPoint, TWContext> : TestFixture<TEntryPoint>
where TEntryPoint : class
where TWContext : DbContext
{
    public Task ExecuteDbContextAsync(Func<TWContext, Task> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetService<TWContext>()));
    }

    public Task ExecuteDbContextAsync(Func<TWContext, ValueTask> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetService<TWContext>()).AsTask());
    }

    public Task ExecuteDbContextAsync(Func<TWContext, IMediator, Task> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetService<TWContext>(), sp.GetService<IMediator>()));
    }

    public Task<T> ExecuteDbContextAsync<T>(Func<TWContext, Task<T>> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetService<TWContext>()));
    }

    public Task<T> ExecuteDbContextAsync<T>(Func<TWContext, ValueTask<T>> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetService<TWContext>()).AsTask());
    }

    public Task<T> ExecuteDbContextAsync<T>(Func<TWContext, IMediator, Task<T>> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetService<TWContext>(), sp.GetService<IMediator>()));
    }

    public Task InsertAsync<T>(params T[] entities) where T : class
    {
        return ExecuteDbContextAsync(db =>
        {
            foreach (var entity in entities)
                db.Set<T>().Add(entity);
            return db.SaveChangesAsync();
        });
    }

    public async Task<T> FindAsync<T, TKey>(TKey id) where T : class, IEntity
    {
        return await ExecuteDbContextAsync(db => db.Set<T>().FindAsync(id).AsTask());
    }
}

public class TestReadFixture<TEntryPoint, TRContext> : TestFixture<TEntryPoint>
where TEntryPoint : class
where TRContext : MongoDbContext
{
    public Task ExecuteReadContextAsync(Func<TRContext, Task> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetRequiredService<TRContext>()));
    }

    public Task<T> ExecuteReadContextAsync<T>(Func<TRContext, Task<T>> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetRequiredService<TRContext>()));
    }

    public async Task InsertMongoDbContextAsync<T>(string collectionName, params T[] entities) where T : class
    {
        await ExecuteReadContextAsync(async db =>
        {
            await db.GetCollection<T>(collectionName).InsertManyAsync(entities.ToList());
        });
    }
}

public class TestFixture<TEntryPoint, TWContext, TRContext> : TestWriteFixture<TEntryPoint, TWContext>
where TEntryPoint : class
where TWContext : DbContext
where TRContext : MongoDbContext
{
    public Task ExecuteReadContextAsync(Func<TRContext, Task> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetRequiredService<TRContext>()));
    }

    public Task<T> ExecuteReadContextAsync<T>(Func<TRContext, Task<T>> action)
    {
        return ExecuteScopeAsync(sp => action(sp.GetRequiredService<TRContext>()));
    }

    public async Task InsertMongoDbContextAsync<T>(string collectionName, params T[] entities) where T : class
    {
        await ExecuteReadContextAsync(async db =>
        {
            await db.GetCollection<T>(collectionName).InsertManyAsync(entities.ToList());
        });
    }
}

public class TestFixtureCore<TEntryPoint> : IAsyncLifetime
where TEntryPoint : class
{
    private Respawner _respawnerDefaultDb;
    private Respawner _respawnerOutboxDb;
    private NpgsqlConnection _defaultDbConnection;
    private NpgsqlConnection _outboxDbConnection;

    public TestFixtureCore(TestFixture<TEntryPoint> integrationTestFixture, ITestOutputHelper outputHelper)
    {
        Fixture = integrationTestFixture;
        integrationTestFixture.Logger = integrationTestFixture.CreateLogger(outputHelper);
    }

    public TestFixture<TEntryPoint> Fixture { get; }

    public async Task InitializeAsync()
    {
        await InitDatabasesAsync();
    }

    public async Task DisposeAsync()
    {
        await ResetDatabasesAsync();
    }

    private async Task InitDatabasesAsync()
    {
        var postgresOptions = Fixture.ServiceProvider.GetService<PostgresOptions>();
        var outboxOptions = Fixture.ServiceProvider.GetService<PostgresOutboxOptions>();

        if (!string.IsNullOrEmpty(outboxOptions?.ConnectionString))
        {
            _outboxDbConnection = new NpgsqlConnection(outboxOptions.ConnectionString);
            await _outboxDbConnection.OpenAsync();
            _respawnerOutboxDb = await Respawner.CreateAsync(_outboxDbConnection, 
                new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
        }

        if (!string.IsNullOrEmpty(postgresOptions?.ConnectionString))
        {
            _defaultDbConnection = new NpgsqlConnection(postgresOptions.ConnectionString);
            await _defaultDbConnection.OpenAsync();
            _respawnerDefaultDb = await Respawner.CreateAsync(_defaultDbConnection, 
                new RespawnerOptions { DbAdapter = DbAdapter.Postgres });

            await SeedDataAsync();
        }
    }

    private async Task ResetDatabasesAsync()
    {
        if (_outboxDbConnection != null)
        {
            await _respawnerOutboxDb.ResetAsync(_outboxDbConnection);
            await _outboxDbConnection.CloseAsync();
        }

        if (_defaultDbConnection != null)
        {
            await _respawnerDefaultDb.ResetAsync(_defaultDbConnection);
            await _defaultDbConnection.CloseAsync();
        }

        await ResetMongoAsync();
        await ResetRabbitMqAsync();
    }

    private async Task ResetMongoAsync(CancellationToken cancellationToken = default)
    {
        var dbClient = new MongoClient(Fixture.MongoDbTestContainer?.GetConnectionString());
        var collections = await dbClient.GetDatabase(TestContainers.MongoContainerConfiguration.Name)
            .ListCollectionsAsync(cancellationToken: cancellationToken);

        foreach (var collection in collections.ToList())
        {
            await dbClient.GetDatabase(TestContainers.MongoContainerConfiguration.Name)
                .DropCollectionAsync(collection["name"].AsString, cancellationToken);
        }
    }

    private async Task ResetRabbitMqAsync(CancellationToken cancellationToken = default)
    {
        // RabbitMQ cleanup is handled by MassTransit test harness automatically
        await Task.CompletedTask;
    }

    private async Task SeedDataAsync()
    {
        using var scope = Fixture.ServiceProvider.CreateScope();
        var seedManager = scope.ServiceProvider.GetService<ISeedManager>();
        if (seedManager != null)
            await seedManager.ExecuteTestSeedAsync();
    }
}

// Base test classes remain the same...
public abstract class TestReadBase<TEntryPoint, TRContext> : TestFixtureCore<TEntryPoint>
    where TEntryPoint : class where TRContext : MongoDbContext
{
    protected TestReadBase(TestReadFixture<TEntryPoint, TRContext> integrationTestFixture, ITestOutputHelper outputHelper = null)
        : base(integrationTestFixture, outputHelper)
    {
        Fixture = integrationTestFixture;
    }

    public TestReadFixture<TEntryPoint, TRContext> Fixture { get; }
}

public abstract class TestWriteBase<TEntryPoint, TWContext> : TestFixtureCore<TEntryPoint>
    where TEntryPoint : class where TWContext : DbContext
{
    protected TestWriteBase(TestWriteFixture<TEntryPoint, TWContext> integrationTestFixture, ITestOutputHelper outputHelper = null)
        : base(integrationTestFixture, outputHelper)
    {
        Fixture = integrationTestFixture;
    }

    public TestWriteFixture<TEntryPoint, TWContext> Fixture { get; }
}

public abstract class TestBase<TEntryPoint, TWContext, TRContext> : TestFixtureCore<TEntryPoint>
    where TEntryPoint : class where TWContext : DbContext where TRContext : MongoDbContext
{
    protected TestBase(TestFixture<TEntryPoint, TWContext, TRContext> integrationTestFixture, ITestOutputHelper outputHelper = null)
        : base(integrationTestFixture, outputHelper)
    {
        Fixture = integrationTestFixture;
    }

    public TestFixture<TEntryPoint, TWContext, TRContext> Fixture { get; }
}