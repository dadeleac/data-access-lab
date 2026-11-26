using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Http;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents;

internal static class ScanIncidentUpdatesEndpoint
{
    public const string DbContextKey = "lab04";

    public static IEndpointRouteBuilder MapIncidentUpdateScan(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(IncidentUpdateScanContract.Route, ExecuteAsync)
            .WithName("ScanIncidentUpdates")
            .WithSummary("Compares tracking, identity resolution and buffering for the same update scan.");

        return endpoints;
    }

    private static async Task<Results<Ok<IncidentUpdateScanResult>, ValidationProblem>> ExecuteAsync(
        string? strategy,
        int? limit,
        [FromKeyedServices(DbContextKey)] OpsBoardDbContext dbContext,
        DbCommandMetrics commandMetrics,
        QueryShapeMetricsInterceptor shapeInterceptor,
        IConfiguration configuration,
        TimeProvider timeProvider,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var selectedStrategy = string.IsNullOrWhiteSpace(strategy)
            ? IncidentUpdateScanContract.Strategies.NoTracking
            : strategy.ToLowerInvariant();
        var selectedLimit = limit ?? IncidentUpdateScanContract.DefaultLimit;
        var errors = new Dictionary<string, string[]>();

        if (!IncidentUpdateScanContract.SupportedStrategies.Contains(selectedStrategy))
        {
            errors[nameof(strategy)] =
                [$"The supported strategies are: {string.Join(", ", IncidentUpdateScanContract.SupportedStrategies)}."];
        }

        if (selectedLimit is < 1 or > IncidentUpdateScanContract.MaximumLimit)
        {
            errors[nameof(limit)] = [$"Limit must be between 1 and {IncidentUpdateScanContract.MaximumLimit}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (dbContext.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException("The Lab 04 query context must use a non-retrying execution strategy.");
        }

        var shape = shapeInterceptor.Start(dbContext);
        try
        {
            commandMetrics.Reset();
            await using var memory = configuration.GetValue<bool>(IncidentUpdateScanContract.MeasureMemoryConfigurationKey)
                ? new ManagedMemorySampler()
                : null;
            var startedAt = timeProvider.GetTimestamp();
            var scan = await IncidentUpdateScanQuery.ExecuteAsync(
                dbContext, selectedStrategy, selectedLimit, memory, cancellationToken);
            var elapsed = timeProvider.GetElapsedTime(startedAt);

            if (memory is not null)
            {
                var measurements = memory.Snapshot();
                response.Headers[IncidentUpdateScanContract.Headers.AllocatedBytes] =
                    measurements.AllocatedBytes.ToString(CultureInfo.InvariantCulture);
                response.Headers[IncidentUpdateScanContract.Headers.BaselineManagedBytes] =
                    measurements.BaselineManagedBytes.ToString(CultureInfo.InvariantCulture);
                response.Headers[IncidentUpdateScanContract.Headers.PeakManagedBytes] =
                    measurements.PeakManagedBytes.ToString(CultureInfo.InvariantCulture);
            }

            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            response.Headers[IncidentUpdateScanContract.Headers.TrackedEntries] =
                dbContext.ChangeTracker.Entries().Count().ToString(CultureInfo.InvariantCulture);
            IncidentMetricsResponse.Write(response, selectedStrategy, commandMetrics, elapsed);
            IncidentMetricsResponse.WriteQueryShape(response, shape);
            return TypedResults.Ok(scan);
        }
        finally
        {
            shapeInterceptor.Stop(dbContext);
        }
    }
}
