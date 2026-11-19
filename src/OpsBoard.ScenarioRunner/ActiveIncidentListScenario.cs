using System.Text.Json;
using OpsBoard.Contracts.Http;

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

    public async Task<ActiveIncidentListReport> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<ActiveIncidentListResult>(
            ActiveIncidentListContract.SupportedLimits.Count * Strategies.Length);

        foreach (var limit in ActiveIncidentListContract.SupportedLimits)
        {
            string? expectedBodySha256 = null;

            foreach (var strategy in Strategies)
            {
                await ExecuteAsync(strategy, limit, cancellationToken);

                var samples = new List<ActiveIncidentListResponse>(capacity: 3);

                for (var iteration = 0; iteration < 3; iteration++)
                {
                    var sample = await ExecuteAsync(strategy, limit, cancellationToken);
                    var expectedQueryCount = strategy switch
                    {
                        ActiveIncidentListContract.Strategies.PerIncident =>
                            1 + (3 * limit) + sample.AssignedCount,
                        ActiveIncidentListContract.Strategies.Batched => 5,
                        ActiveIncidentListContract.Strategies.Projected => 1,
                        _ => throw new System.Diagnostics.UnreachableException()
                    };
                    expectedBodySha256 ??= sample.BodySha256;
                    ScenarioSupport.ValidateSample(
                        sample, limit, iteration + 1, expectedQueryCount, expectedBodySha256);
                    samples.Add(sample);
                }

                var representative = samples[0];
                results.Add(new ActiveIncidentListResult(
                    strategy,
                    limit,
                    representative.ItemCount,
                    representative.QueryCount,
                    ScenarioSupport.Median(
                        samples.Select(sample => sample.QueryExecutionMilliseconds)),
                    ScenarioSupport.Median(
                        samples.Select(sample => sample.DataAccessMilliseconds)),
                    representative.BodySha256));
            }
        }

        return new ActiveIncidentListReport(
            ScenarioName,
            timeProvider.GetUtcNow(),
            results);
    }

    private async Task<ActiveIncidentListResponse> ExecuteAsync(
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

        return new ActiveIncidentListResponse(
            strategy,
            items.GetArrayLength(),
            assignedCount,
            ScenarioSupport.ReadIntHeader(
                response,
                IncidentMetricsHeaders.QueryCount),
            ScenarioSupport.ReadDoubleHeader(
                response,
                IncidentMetricsHeaders.QueryDuration),
            ScenarioSupport.ReadDoubleHeader(
                response,
                IncidentMetricsHeaders.DataAccessElapsed),
            ScenarioSupport.Hash(body));
    }
}

internal sealed record ActiveIncidentListResponse(
    string Strategy,
    int ItemCount,
    int AssignedCount,
    int QueryCount,
    double QueryExecutionMilliseconds,
    double DataAccessMilliseconds,
    string BodySha256) : IScenarioSample;
