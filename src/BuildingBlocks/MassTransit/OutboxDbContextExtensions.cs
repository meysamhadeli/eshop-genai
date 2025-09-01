using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.MassTransit;

public static class OutboxDbContextExtensions
{
    public static IServiceCollection AddMassTransitOutboxDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var outboxOptions = configuration.GetSection(nameof(PostgresOutboxOptions)).Get<PostgresOutboxOptions>();

        if (outboxOptions == null)
        {
            throw new InvalidOperationException("PostgresOutboxOptions configuration section is required");
        }

        if (string.IsNullOrEmpty(outboxOptions.ConnectionString))
        {
            throw new InvalidOperationException("PostgresOutboxOptions.ConnectionString is required");
        }

        services.AddDbContext<OutboxDbContext>(options =>
        {
            options.UseNpgsql(outboxOptions.ConnectionString);

            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddHostedService<OutboxDatabaseInitializer>();

        return services;
    }
}

public class OutboxDatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDatabaseInitializer> _logger;

    public OutboxDatabaseInitializer(IServiceProvider serviceProvider, ILogger<OutboxDatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

            _logger.LogInformation("Ensuring outbox database exists and migrations are applied...");

            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Outbox database migrations applied successfully");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize outbox database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}