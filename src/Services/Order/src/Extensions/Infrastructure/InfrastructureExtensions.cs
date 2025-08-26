using BuildingBlocks.Caching;
using BuildingBlocks.Core;
using BuildingBlocks.EFCore;
using BuildingBlocks.Jwt;
using BuildingBlocks.Mapster;
using BuildingBlocks.MassTransit;
using BuildingBlocks.OpenApi;
using BuildingBlocks.PersistMessageProcessor;
using BuildingBlocks.ProblemDetails;
using BuildingBlocks.Web;
using Figgle;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Order.Data;

namespace Order.Extensions.Infrastructure;


public static class InfrastructureExtensions
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var env = builder.Environment;
        
        var appOptions = builder.Services.GetOptions<AppOptions>(nameof(AppOptions));
        Console.WriteLine(FiggleFonts.Standard.Render(appOptions.Name));
        
        builder.Services.AddCors(options =>
                                 {
                                     options.AddPolicy("AllowFrontend",
                                         builder => builder
                                             .WithOrigins(appOptions.UiUrl)
                                             .AllowAnyMethod()
                                             .AllowAnyHeader()
                                             .AllowCredentials());
                                 });

        builder.AddServiceDefaults();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddCustomMediatR();
        builder.Services.AddProblemDetails();
        builder.Services.AddJwt();

        builder.AddCustomDbContext<OrderDbContext>(nameof(Order));
        builder.AddPersistMessageProcessor(nameof(PersistMessage));
        builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();
        builder.Services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddAspnetOpenApi();
        builder.Services.AddCustomVersioning();
        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddCustomMapster(typeof(Program).Assembly);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCustomMassTransit(env, TransportType.RabbitMq, typeof(Program).Assembly);
        
        builder.Services.AddCustomHybridCaching();
        builder.Services.AddGrpcClients();

        return builder;
    }


    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        var env = app.Environment;
        var appOptions = app.GetOptions<AppOptions>(nameof(AppOptions));
        
        app.UseCors("AllowFrontend");
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseServiceDefaults();

        app.UseCustomProblemDetails();
        app.UseCorrelationId();
        app.UseMigration<OrderDbContext>();

        app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));

        if (env.IsDevelopment())
        {
            app.UseAspnetOpenApi();
        }

        return app;
    }
}