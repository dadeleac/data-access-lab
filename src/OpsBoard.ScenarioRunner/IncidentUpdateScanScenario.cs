using System.Globalization;
using System.Text.Json;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Contracts.Http;

internal sealed class IncidentUpdateScanScenario(HttpClient httpClient, TimeProvider timeProvider)
{
    private const string ScenarioName = "incident-update-scan";

    public async Task<IncidentUpdateScanReport> RunAsync(
        int datasetUpdateCount,
        string? measuredStrategy = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> strategies = measuredStrategy is null
            ? IncidentUpdateScanContract.SupportedStrategies
            : [measuredStrategy];
        int[] limits = datasetUpdateCount == DatasetOptions.LargeUpdateCount
            ? [1_000, 10_000, DatasetOptions.LargeUpdateCount]
            : [1_000, DatasetOptions.SmallUpdateCount];
        var results = new List<IncidentUpdateScanMeasurement>(limits.Length * strategies.Count);

        foreach (var limit in limits)
        {
            string? expectedHash = null;
            int? expectedColumns = null;

            foreach (var strategy in strategies)
            {
                await ExecuteAsync(strategy, limit, measuredStrategy is not null, cancellationToken);
                var samples = new List<ScanSample>(capacity: 3);

                for (var sampleNumber = 1; sampleNumber <= 3; sampleNumber++)
                {
                    var sample = await ExecuteAsync(strategy, limit, measuredStrategy is not null, cancellationToken);
                    expectedHash ??= sample.BodySha256;
                    expectedColumns ??= sample.ColumnCount;
                    ScenarioSupport.ValidateSample(sample, limit, sampleNumber, 1, expectedHash);
                    ValidateShape(sample, expectedColumns.Value, limit, sampleNumber);
                    samples.Add(sample);
                }

                var first = samples[0];
                results.Add(new IncidentUpdateScanMeasurement(
                    strategy, limit, first.ItemCount, first.DistinctAuthorCount, first.QueryCount,
                    first.RowCount, first.ColumnCount, first.MaterializedEntityCount, first.TrackedEntries,
                    ScenarioSupport.Median(samples.Select(sample => sample.QueryExecutionMilliseconds)),
                    ScenarioSupport.Median(samples.Select(sample => sample.DataAccessMilliseconds)),
                    first.BodySha256,
                    measuredStrategy is null ? null : new IncidentUpdateScanMemory(
                        (long)ScenarioSupport.Median(samples.Select(sample => (double)sample.Memory.AllocatedBytes)),
                        (long)ScenarioSupport.Median(samples.Select(sample => (double)sample.Memory.BaselineManagedBytes)),
                        (long)ScenarioSupport.Median(samples.Select(sample => (double)sample.Memory.PeakManagedBytes)))));
            }
        }

        return new IncidentUpdateScanReport(ScenarioName, timeProvider.GetUtcNow(), results);
    }

    private async Task<ScanSample> ExecuteAsync(
        string strategy, int limit, bool measureMemory, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            IncidentUpdateScanContract.CreateRequestPath(strategy, limit), cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var scan = JsonSerializer.Deserialize<IncidentUpdateScanResult>(body, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("The scan did not return a JSON result.");
        var memory = measureMemory
            ? new MemorySample(
                ReadLongHeader(response, IncidentUpdateScanContract.Headers.AllocatedBytes),
                ReadLongHeader(response, IncidentUpdateScanContract.Headers.BaselineManagedBytes),
                ReadLongHeader(response, IncidentUpdateScanContract.Headers.PeakManagedBytes))
            : default;

        return new ScanSample(strategy, scan.ItemCount, scan.DistinctAuthorCount,
            ScenarioSupport.ReadIntHeader(response, IncidentMetricsHeaders.QueryCount),
            ScenarioSupport.ReadIntHeader(response, IncidentMetricsHeaders.RowCount),
            ScenarioSupport.ReadIntHeader(response, IncidentMetricsHeaders.ColumnCount),
            ScenarioSupport.ReadIntHeader(response, IncidentMetricsHeaders.MaterializedEntityCount),
            ScenarioSupport.ReadIntHeader(response, IncidentUpdateScanContract.Headers.TrackedEntries),
            ScenarioSupport.ReadDoubleHeader(response, IncidentMetricsHeaders.QueryDuration),
            ScenarioSupport.ReadDoubleHeader(response, IncidentMetricsHeaders.DataAccessElapsed),
            ScenarioSupport.Hash(body), memory);
    }

    private static void ValidateShape(ScanSample sample, int expectedColumns, int limit, int sampleNumber)
    {
        var isTracking = sample.Strategy == IncidentUpdateScanContract.Strategies.Tracking;
        var resolvesIdentity = isTracking || sample.Strategy == IncidentUpdateScanContract.Strategies.IdentityResolution;
        var uniqueEntities = sample.ItemCount + sample.DistinctAuthorCount;
        var expectedMaterializations = resolvesIdentity ? uniqueEntities : 2 * sample.ItemCount;
        var expectedTracked = isTracking ? uniqueEntities : 0;

        if (sample.RowCount != limit || sample.ColumnCount != expectedColumns || sample.ColumnCount <= 0
            || sample.DistinctAuthorCount <= 0 || sample.DistinctAuthorCount >= limit
            || sample.MaterializedEntityCount != expectedMaterializations || sample.TrackedEntries != expectedTracked)
        {
            throw new InvalidOperationException(
                $"Strategy '{sample.Strategy}', limit {limit}, sample {sampleNumber}: "
                + $"observed {sample.RowCount} rows, {sample.ColumnCount} columns, {sample.DistinctAuthorCount} authors, "
                + $"{sample.MaterializedEntityCount} entities created and {sample.TrackedEntries} tracked. "
                + $"Expected {limit} rows, {expectedColumns} columns, repeated authors, "
                + $"{expectedMaterializations} entities created and {expectedTracked} tracked.");
        }
    }

    private static long ReadLongHeader(HttpResponseMessage response, string name) =>
        long.Parse(ScenarioSupport.ReadHeader(response, name), CultureInfo.InvariantCulture);

    private readonly record struct MemorySample(long AllocatedBytes, long BaselineManagedBytes, long PeakManagedBytes);

    private sealed record ScanSample(
        string Strategy,
        int ItemCount,
        int DistinctAuthorCount,
        int QueryCount,
        int RowCount,
        int ColumnCount,
        int MaterializedEntityCount,
        int TrackedEntries,
        double QueryExecutionMilliseconds,
        double DataAccessMilliseconds,
        string BodySha256,
        MemorySample Memory) : IScenarioSample;
}
