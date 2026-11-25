using System.Text.Json;
using OpsBoard.Contracts.Http;

internal sealed class ActiveIncidentSummaryScenario(HttpClient httpClient, TimeProvider timeProvider)
{
    private const string ScenarioName = "active-incident-summary";

    private static readonly string[] Strategies =
    [
        ActiveIncidentSummaryContract.Strategies.FullEntity,
        ActiveIncidentSummaryContract.Strategies.Projected
    ];

    public async Task<ActiveIncidentSummaryReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var limits = ActiveIncidentSummaryContract.SupportedLimits;
        var results = new List<ActiveIncidentSummaryResult>(limits.Count * Strategies.Length);

        foreach (var limit in limits)
        {
            string? expectedBodySha256 = null;

            foreach (var strategy in Strategies)
            {
                await ExecuteAsync(strategy, limit, cancellationToken);

                var samples = new List<ActiveIncidentSummarySample>(capacity: 3);
                for (var sampleNumber = 1; sampleNumber <= 3; sampleNumber++)
                {
                    var sample = await ExecuteAsync(strategy, limit, cancellationToken);
                    expectedBodySha256 ??= sample.BodySha256;
                    ScenarioSupport.ValidateSample(sample, limit, sampleNumber, 1, expectedBodySha256);
                    ValidateQueryShape(sample, limit, sampleNumber);
                    samples.Add(sample);
                }

                var representative = samples[0];
                results.Add(new ActiveIncidentSummaryResult(
                    strategy,
                    limit,
                    representative.ItemCount,
                    representative.QueryCount,
                    representative.RowCount,
                    representative.ColumnCount,
                    representative.MaterializedEntityCount,
                    ScenarioSupport.Median(samples.Select(sample => sample.QueryExecutionMilliseconds)),
                    ScenarioSupport.Median(samples.Select(sample => sample.DataAccessMilliseconds)),
                    representative.BodySha256));
            }
        }

        return new ActiveIncidentSummaryReport(ScenarioName, timeProvider.GetUtcNow(), results);
    }

    private async Task<ActiveIncidentSummarySample> ExecuteAsync(
        string strategy,
        int limit,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            ActiveIncidentSummaryContract.CreateRequestPath(strategy, limit), cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var items = document.RootElement;
        var assignedCount = 0;

        foreach (var item in items.EnumerateArray())
        {
            if (item.EnumerateObject().Count() != ActiveIncidentSummaryContract.FieldCount)
            {
                throw new InvalidOperationException(
                    $"Strategy '{strategy}', limit {limit}: expected eight fields in each incident summary.");
            }

            if (item.GetProperty("assignee").ValueKind != JsonValueKind.Null)
            {
                assignedCount++;
            }
        }

        return new ActiveIncidentSummarySample(
            strategy,
            items.GetArrayLength(),
            ScenarioSupport.ReadIntHeader(response, IncidentMetricsHeaders.QueryCount),
            ScenarioSupport.ReadIntHeader(response, ActiveIncidentSummaryContract.Headers.RowCount),
            ScenarioSupport.ReadIntHeader(response, ActiveIncidentSummaryContract.Headers.ColumnCount),
            ScenarioSupport.ReadIntHeader(response, ActiveIncidentSummaryContract.Headers.MaterializedEntityCount),
            assignedCount,
            ScenarioSupport.ReadDoubleHeader(response, IncidentMetricsHeaders.QueryDuration),
            ScenarioSupport.ReadDoubleHeader(response, IncidentMetricsHeaders.DataAccessElapsed),
            ScenarioSupport.Hash(body));
    }

    private static void ValidateQueryShape(ActiveIncidentSummarySample sample, int limit, int sampleNumber)
    {
        var isFullEntity = sample.Strategy == ActiveIncidentSummaryContract.Strategies.FullEntity;
        var expectedEntityCount = isFullEntity ? (3 * sample.ItemCount) + sample.AssignedCount : 0;
        var projectedColumnCount = ActiveIncidentSummaryContract.FieldCount;
        var hasExpectedColumnCount = isFullEntity
            ? sample.ColumnCount > projectedColumnCount
            : sample.ColumnCount == projectedColumnCount;

        if (sample.RowCount != sample.ItemCount || !hasExpectedColumnCount
            || sample.MaterializedEntityCount != expectedEntityCount)
        {
            var expectedColumns = isFullEntity ? $"more than {projectedColumnCount}" : $"{projectedColumnCount}";
            throw new InvalidOperationException(
                $"Strategy '{sample.Strategy}', limit {limit}, sample {sampleNumber}: "
                + $"read {sample.RowCount} rows and {sample.ColumnCount} columns and materialized "
                + $"{sample.MaterializedEntityCount} entities. Expected {sample.ItemCount} rows, "
                + $"{expectedColumns} columns and {expectedEntityCount} entities.");
        }
    }

    private sealed record ActiveIncidentSummarySample(
        string Strategy,
        int ItemCount,
        int QueryCount,
        int RowCount,
        int ColumnCount,
        int MaterializedEntityCount,
        int AssignedCount,
        double QueryExecutionMilliseconds,
        double DataAccessMilliseconds,
        string BodySha256) : IScenarioSample;
}
