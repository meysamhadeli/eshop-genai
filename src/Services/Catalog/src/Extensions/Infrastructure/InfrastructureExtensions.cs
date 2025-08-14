using BuildingBlocks.EFCore;
using BuildingBlocks.Exception;
using BuildingBlocks.Jwt;
using BuildingBlocks.Mapster;
using BuildingBlocks.OpenApi;
using BuildingBlocks.ProblemDetails;
using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.GrpcServer.Services;
using Figgle;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Extensions.Infrastructure;


public static class InfrastructureExtensions
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var env = builder.Environment;

        builder.AddServiceDefaults();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddCustomMediatR();
        builder.Services.AddProblemDetails();
        builder.Services.AddJwt();

        var appOptions = builder.Services.GetOptions<AppOptions>(nameof(AppOptions));
        Console.WriteLine(FiggleFonts.Standard.Render(appOptions.Name));

        builder.AddCustomDbContext<CatalogDbContext>(nameof(Catalog));
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
        
        builder.Services.AddEasyCaching(options => { options.UseInMemory(configuration, "mem"); });

        return builder;
    }


    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        var env = app.Environment;
        var appOptions = app.GetOptions<AppOptions>(nameof(AppOptions));

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseServiceDefaults();

        app.UseCustomProblemDetails();
        app.UseCorrelationId();
        app.UseMigration<CatalogDbContext>();
        app.MapGrpcService<CatalogGrpcServices>();

        app.MapGet("/", x => x.Response.WriteAsync(appOptions.Name));

        if (env.IsDevelopment())
        {
            app.UseAspnetOpenApi();
        }

        return app;
    }
}