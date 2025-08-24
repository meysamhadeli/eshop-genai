using Basket.GrpcServer.Services;
using Basket.Infrastructure.Redis;
using Booking.Extensions.Infrastructure;
using BuildingBlocks.Caching;
using BuildingBlocks.Exception;
using BuildingBlocks.Jwt;
using BuildingBlocks.Mapster;
using BuildingBlocks.OpenApi;
using BuildingBlocks.ProblemDetails;
using BuildingBlocks.Web;
using Figgle;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Basket.Extensions.Infrastructure;

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

        builder.Services.AddCustomHybridCaching();
        builder.Services.AddScoped<IBasketRedisService, BasketRedisService>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddAspnetOpenApi();
        builder.Services.AddCustomVersioning();
        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddCustomMapster(typeof(Program).Assembly);
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddGrpc(options => 
         {
             options.Interceptors.Add<GrpcExceptionInterceptor>();
         });
        builder.Services.AddGrpcClients();

        return builder;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        var env = app.Environment;
        var appOptions = app.GetOptions<AppOptions>(nameof(AppOptions));
        
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseServiceDefaults();

        app.UseCustomProblemDetails();
        app.UseCorrelationId();
        app.MapGrpcService<BasketGrpcServices>();
        
        app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));

        if (env.IsDevelopment())
        {
            app.UseAspnetOpenApi();
        }

        return app;
    }
}