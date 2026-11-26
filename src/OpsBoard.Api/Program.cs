using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Api.Incidents;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Hosting;
using OpsBoard.Contracts.Http;
using OpsBoard.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration
    .GetConnectionString(OpsBoardResourceNames.Database)
    ?? throw new InvalidOperationException(
        $"The '{OpsBoardResourceNames.Database}' connection string is required.");

builder.Services.AddScoped<DbCommandMetrics>();
builder.Services.AddScoped<DbCommandMetricsInterceptor>();
builder.Services.AddSingleton<QueryShapeMetricsInterceptor>();
builder.Services.AddDbContext<OpsBoardDbContext>(ConfigureDbContext);
builder.EnrichNpgsqlDbContext<OpsBoardDbContext>();
builder.Services.AddKeyedScoped<OpsBoardDbContext>(ScanIncidentUpdatesEndpoint.DbContextKey, (services, _) =>
{
    var options = new DbContextOptionsBuilder<OpsBoardDbContext>();
    ConfigureDbContext(services, options);
    return new OpsBoardDbContext(options.Options);
});
builder.Services.AddScoped<IncidentListItemLoader>();
builder.Services.AddScoped<PerIncidentEnrichmentQuery>();
builder.Services.AddScoped<BatchedIncidentListQuery>();
builder.Services.AddScoped<ProjectedIncidentListQuery>();
builder.Services.AddScoped<SingleQueryIncidentDetailsQuery>();
builder.Services.AddScoped<SplitQueryIncidentDetailsQuery>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet(
        ApiDocumentationContract.RootRoute,
        () => TypedResults.Redirect(ApiDocumentationContract.ScalarRoute))
    .ExcludeFromDescription();

app.MapActiveIncidents();
app.MapActiveIncidentDetails();
app.MapActiveIncidentSummaries();
app.MapIncidentUpdateScan();
app.MapDefaultEndpoints();

app.Run();

void ConfigureDbContext(IServiceProvider services, DbContextOptionsBuilder options)
{
    options.UseNpgsql(connectionString).AddInterceptors(
        services.GetRequiredService<DbCommandMetricsInterceptor>(),
        services.GetRequiredService<QueryShapeMetricsInterceptor>());
}

public partial class Program;
