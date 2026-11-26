using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Contracts.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var datasetOptions = builder.Configuration
    .GetSection(DatasetOptions.ConfigurationSectionName)
    .Get<DatasetOptions>();
var scenarioOptions = builder.Configuration
    .GetSection(ScenarioOptions.ConfigurationSectionName)
    .Get<ScenarioOptions>();

builder.AddServiceDefaults();
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("Polly", LogLevel.Warning);
builder.Services.AddHttpClient(
    OpsBoardResourceNames.Api,
    client => client.BaseAddress = new Uri(OpsBoardResourceNames.ApiServiceUri));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
var httpClient = httpClientFactory.CreateClient(OpsBoardResourceNames.Api);

var activeIncidentList = new ActiveIncidentListScenario(httpClient, timeProvider);
var activeIncidentDetails = new ActiveIncidentDetailsScenario(
    httpClient, timeProvider, datasetOptions?.IsReferenceDataset == true);
var activeIncidentSummary = new ActiveIncidentSummaryScenario(httpClient, timeProvider);
var incidentUpdateScan = new IncidentUpdateScanScenario(httpClient, timeProvider);
var updateCount = datasetOptions?.ExpectedUpdateCount ?? DatasetOptions.SmallUpdateCount;
object report = scenarioOptions?.Lab04Strategy is { } strategy
    ? new { Lab04 = await incidentUpdateScan.RunAsync(updateCount, strategy) }
    : new ScenarioRunReport(
        timeProvider.GetUtcNow(),
        await activeIncidentList.RunAsync(),
        await activeIncidentDetails.RunAsync(),
        await activeIncidentSummary.RunAsync(),
        await incidentUpdateScan.RunAsync(updateCount));

Console.WriteLine(JsonSerializer.Serialize(
    report,
    new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    }));
