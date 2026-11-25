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
builder.Services.AddSingleton<IncidentSummaryMetricsInterceptor>();
builder.Services.AddDbContext<OpsBoardDbContext>((services, options) =>
    options
        .UseNpgsql(connectionString)
        .AddInterceptors(
            services.GetRequiredService<DbCommandMetricsInterceptor>(),
            services.GetRequiredService<IncidentSummaryMetricsInterceptor>()));
builder.EnrichNpgsqlDbContext<OpsBoardDbContext>();
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
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
