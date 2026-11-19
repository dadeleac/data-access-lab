using System.Text.Json;
using OpsBoard.Contracts.Http;

internal sealed class ActiveIncidentDetailsScenario(
    HttpClient httpClient,
    TimeProvider timeProvider,
    bool verifyReferenceCardinality)
{
    private const string ScenarioName = "active-incident-details";

    private static readonly string[] Strategies =
    [
        ActiveIncidentDetailsContract.Strategies.SingleQuery,
        ActiveIncidentDetailsContract.Strategies.SplitQuery
    ];

    private static readonly (int Limit, long SingleQueryRows, long SplitQueryRows)[] ReferenceCases =
    [
        (10, 454, 291),
        (50, 1_061, 684),
        (100, 1_706, 1_112)
    ];

    public async Task<ActiveIncidentDetailsReport> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<ActiveIncidentDetailsResult>(
            ReferenceCases.Length * Strategies.Length);

        foreach (var (limit, singleQueryRows, splitQueryRows) in ReferenceCases)
        {
            string? expectedBodySha256 = null;

            foreach (var strategy in Strategies)
            {
                await ExecuteAsync(strategy, limit, cancellationToken);

                var isSingleQuery = strategy == ActiveIncidentDetailsContract.Strategies.SingleQuery;
                var expectedQueryCount = isSingleQuery ? 1 : 3;
                var referenceRows = isSingleQuery ? singleQueryRows : splitQueryRows;
                var samples = new List<ActiveIncidentDetailsResponse>(capacity: 3);

                for (var iteration = 0; iteration < 3; iteration++)
                {
                    var sample = await ExecuteAsync(strategy, limit, cancellationToken);
                    expectedBodySha256 ??= sample.BodySha256;
                    ScenarioSupport.ValidateSample(
                        sample, limit, iteration + 1, expectedQueryCount, expectedBodySha256);

                    if (verifyReferenceCardinality && sample.ExpectedStructuralRows != referenceRows)
                    {
                        throw new InvalidOperationException(
                            $"Strategy '{strategy}', limit {limit}, sample {iteration + 1}: "
                            + $"calculated {sample.ExpectedStructuralRows} structural rows for the reference dataset; "
                            + $"expected {referenceRows}.");
                    }

                    samples.Add(sample);
                }

                var representative = samples[0];
                results.Add(new ActiveIncidentDetailsResult(
                    strategy,
                    limit,
                    representative.ItemCount,
                    representative.QueryCount,
                    representative.ExpectedStructuralRows,
                    ScenarioSupport.Median(
                        samples.Select(sample => sample.QueryExecutionMilliseconds)),
                    ScenarioSupport.Median(
                        samples.Select(sample => sample.DataAccessMilliseconds)),
                    representative.BodySha256));
            }
        }

        return new ActiveIncidentDetailsReport(
            ScenarioName,
            timeProvider.GetUtcNow(),
            results);
    }

    private async Task<ActiveIncidentDetailsResponse> ExecuteAsync(
        string strategy,
        int limit,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            ActiveIncidentDetailsContract.CreateRequestPath(strategy, limit),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var items = document.RootElement;
        var expectedSingleRows = 0L;
        long expectedSplitRows = items.GetArrayLength();

        foreach (var item in items.EnumerateArray())
        {
            var tagCount = item.GetProperty("tags").GetArrayLength();
            var updateCount = item.GetProperty("updates").GetArrayLength();
            expectedSingleRows += (long)Math.Max(1, tagCount) * Math.Max(1, updateCount);
            expectedSplitRows += (long)tagCount + updateCount;
        }

        return new ActiveIncidentDetailsResponse(
            strategy,
            items.GetArrayLength(),
            ScenarioSupport.ReadIntHeader(
                response,
                IncidentMetricsHeaders.QueryCount),
            strategy == ActiveIncidentDetailsContract.Strategies.SingleQuery
                ? expectedSingleRows
                : expectedSplitRows,
            ScenarioSupport.ReadDoubleHeader(
                response,
                IncidentMetricsHeaders.QueryDuration),
            ScenarioSupport.ReadDoubleHeader(
                response,
                IncidentMetricsHeaders.DataAccessElapsed),
            ScenarioSupport.Hash(body));
    }
}

internal sealed record ActiveIncidentDetailsResponse(
    string Strategy,
    int ItemCount,
    int QueryCount,
    long ExpectedStructuralRows,
    double QueryExecutionMilliseconds,
    double DataAccessMilliseconds,
    string BodySha256) : IScenarioSample;
