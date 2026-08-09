using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpsBoard.Contracts.Hosting;
using OpsBoard.Contracts.Http;

var builder = Host.CreateApplicationBuilder(args);

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
var runner = new ActiveIncidentListScenario(
    httpClientFactory.CreateClient(OpsBoardResourceNames.Api),
    timeProvider);

var report = await runner.RunAsync();

Console.WriteLine(JsonSerializer.Serialize(
    report,
    new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    }));

internal sealed class ActiveIncidentListScenario(
    HttpClient httpClient,
    TimeProvider timeProvider)
{
    private const string ScenarioName = "active-incident-list";

    private static readonly string[] Strategies =
    [
        ActiveIncidentListContract.Strategies.PerIncident,
        ActiveIncidentListContract.Strategies.Batched,
        ActiveIncidentListContract.Strategies.Projected
    ];

    public async Task<ScenarioReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ScenarioResult>(
            ActiveIncidentListContract.SupportedLimits.Count * Strategies.Length);

        foreach (var limit in ActiveIncidentListContract.SupportedLimits)
        {
            var responses = new List<ScenarioResponse>(Strategies.Length);

            foreach (var strategy in Strategies)
            {
                await ExecuteAsync(strategy, limit, cancellationToken);

                var samples = new List<ScenarioResponse>(capacity: 3);

                for (var iteration = 0; iteration < 3; iteration++)
                {
                    samples.Add(await ExecuteAsync(
                        strategy,
                        limit,
                        cancellationToken));
                }

                EnsureStableQueryCount(samples);
                var representative = samples[0];
                responses.Add(representative);
                results.Add(new ScenarioResult(
                    strategy,
                    limit,
                    representative.ItemCount,
                    representative.QueryCount,
                    Median(samples.Select(sample => sample.QueryExecutionMilliseconds)),
                    Median(samples.Select(sample => sample.DataAccessMilliseconds)),
                    representative.BodySha256));
            }

            EnsureEquivalentBodies(responses, limit);
            EnsureExpectedQueryCounts(responses, limit);
        }

        return new ScenarioReport(
            ScenarioName,
            timeProvider.GetUtcNow(),
            results);
    }

    private async Task<ScenarioResponse> ExecuteAsync(
        string strategy,
        int limit,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            ActiveIncidentListContract.CreateRequestPath(strategy, limit),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var items = document.RootElement;
        var assignedCount = items
            .EnumerateArray()
            .Count(item => item.GetProperty("assignee").ValueKind != JsonValueKind.Null);

        return new ScenarioResponse(
            strategy,
            items.GetArrayLength(),
            assignedCount,
            ReadIntHeader(response, ActiveIncidentListContract.Headers.QueryCount),
            ReadDoubleHeader(response, ActiveIncidentListContract.Headers.QueryDuration),
            ReadDoubleHeader(
                response,
                ActiveIncidentListContract.Headers.DataAccessElapsed),
            Convert.ToHexStringLower(SHA256.HashData(body)));
    }

    private static int ReadIntHeader(HttpResponseMessage response, string name) =>
        int.Parse(ReadHeader(response, name), CultureInfo.InvariantCulture);

    private static double ReadDoubleHeader(HttpResponseMessage response, string name) =>
        double.Parse(ReadHeader(response, name), CultureInfo.InvariantCulture);

    private static string ReadHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.Single()
            : throw new InvalidOperationException(
                $"The API response did not contain the required '{name}' header.");

    private static void EnsureStableQueryCount(IReadOnlyList<ScenarioResponse> samples)
    {
        var expected = samples[0].QueryCount;

        if (samples.Any(sample => sample.QueryCount != expected))
        {
            throw new InvalidOperationException(
                $"The '{samples[0].Strategy}' strategy produced an unstable query count.");
        }
    }

    private static void EnsureEquivalentBodies(
        IReadOnlyList<ScenarioResponse> responses,
        int limit)
    {
        var expectedHash = responses[0].BodySha256;

        if (responses.Any(response => response.BodySha256 != expectedHash))
        {
            throw new InvalidOperationException(
                $"The strategies returned different response bodies for limit {limit}.");
        }
    }

    private static void EnsureExpectedQueryCounts(
        IReadOnlyList<ScenarioResponse> responses,
        int limit)
    {
        var perIncident = responses.Single(
            response =>
                response.Strategy == ActiveIncidentListContract.Strategies.PerIncident);
        var batched = responses.Single(
            response => response.Strategy == ActiveIncidentListContract.Strategies.Batched);
        var projected = responses.Single(
            response => response.Strategy == ActiveIncidentListContract.Strategies.Projected);

        var expectedPerIncident =
            1 + (3 * perIncident.ItemCount) + perIncident.AssignedCount;
        var expectedBatched = perIncident.ItemCount == 0 ? 1 : 5;

        if (perIncident.QueryCount != expectedPerIncident)
        {
            throw new InvalidOperationException(
                $"The per-incident strategy executed {perIncident.QueryCount} queries "
                + $"for limit {limit}; expected {expectedPerIncident}.");
        }

        if (batched.QueryCount != expectedBatched)
        {
            throw new InvalidOperationException(
                $"The batched strategy executed {batched.QueryCount} queries "
                + $"for limit {limit}; expected {expectedBatched}.");
        }

        if (projected.QueryCount != 1)
        {
            throw new InvalidOperationException(
                $"The projected strategy executed {projected.QueryCount} queries "
                + $"for limit {limit}; expected 1.");
        }
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return ordered[ordered.Length / 2];
    }
}

internal sealed record ScenarioResponse(
    string Strategy,
    int ItemCount,
    int AssignedCount,
    int QueryCount,
    double QueryExecutionMilliseconds,
    double DataAccessMilliseconds,
    string BodySha256);

internal sealed record ScenarioResult(
    string Strategy,
    int Limit,
    int ItemCount,
    int QueryCount,
    double MedianQueryExecutionMilliseconds,
    double MedianDataAccessMilliseconds,
    string BodySha256);

internal sealed record ScenarioReport(
    string Scenario,
    DateTimeOffset CapturedAt,
    IReadOnlyList<ScenarioResult> Results);
