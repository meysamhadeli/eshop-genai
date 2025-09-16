using BuildingBlocks.Web;
using Recommendation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddMinimalEndpoints(assemblies: typeof(Program).Assembly);
builder.AddInfrastructure();

var app = builder.Build();

app.MapMinimalEndpoints();
app.UseInfrastructure();

app.Run();