using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Http;

namespace OpsBoard.Api.Incidents;

public static class ListActiveIncidentsEndpoint
{
    private const string QueryTimingMetricName = "db-queries";
    private const string DataAccessTimingMetricName = "data-access";

    public static IEndpointRouteBuilder MapActiveIncidents(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                ActiveIncidentListContract.Route,
                ExecuteAsync)
            .WithName("ListActiveIncidents")
            .WithSummary("Lists active incidents using a selectable data-access strategy.")
            .WithDescription(
                "Compares per-incident enrichment, explicit batched loading, "
                + "and a composed projection while preserving the response body. "
                + "The selectable strategy is part of the laboratory, not a recommended "
                + "production API contract.");

        return endpoints;
    }

    private static async Task<Results<
        Ok<IReadOnlyList<IncidentListItem>>,
        ValidationProblem>> ExecuteAsync(
        [Description(ActiveIncidentListContract.StrategyParameterDescription)]
        string? strategy,
        [Description(ActiveIncidentListContract.LimitParameterDescription)]
        int? limit,
        PerIncidentEnrichmentQuery perIncidentQuery,
        BatchedIncidentListQuery batchedQuery,
        ProjectedIncidentListQuery projectedQuery,
        DbCommandMetrics commandMetrics,
        TimeProvider timeProvider,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (!IncidentListRequest.TryCreate(
                strategy,
                limit,
                out var request,
                out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        IIncidentListQuery query = request!.Strategy switch
        {
            IncidentListStrategy.PerIncident => perIncidentQuery,
            IncidentListStrategy.Batched => batchedQuery,
            IncidentListStrategy.Projected => projectedQuery,
            _ => throw new UnreachableException()
        };

        commandMetrics.Reset();
        var startedAt = timeProvider.GetTimestamp();
        var items = await query.ExecuteAsync(request.Limit, cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        WriteMetricsHeaders(response, request.Strategy, commandMetrics, elapsed);
        return TypedResults.Ok(items);
    }

    private static void WriteMetricsHeaders(
        HttpResponse response,
        IncidentListStrategy strategy,
        DbCommandMetrics commandMetrics,
        TimeSpan elapsed)
    {
        var queryExecutionMilliseconds = commandMetrics.Elapsed.TotalMilliseconds;
        var dataAccessMilliseconds = elapsed.TotalMilliseconds;

        response.Headers[ActiveIncidentListContract.Headers.Strategy] = strategy switch
        {
            IncidentListStrategy.PerIncident =>
                ActiveIncidentListContract.Strategies.PerIncident,
            IncidentListStrategy.Batched =>
                ActiveIncidentListContract.Strategies.Batched,
            IncidentListStrategy.Projected =>
                ActiveIncidentListContract.Strategies.Projected,
            _ => throw new UnreachableException()
        };
        response.Headers[ActiveIncidentListContract.Headers.QueryCount] =
            commandMetrics.CommandCount
            .ToString(CultureInfo.InvariantCulture);
        response.Headers[ActiveIncidentListContract.Headers.QueryDuration] =
            queryExecutionMilliseconds
            .ToString("F2", CultureInfo.InvariantCulture);
        response.Headers[ActiveIncidentListContract.Headers.DataAccessElapsed] =
            dataAccessMilliseconds
            .ToString("F2", CultureInfo.InvariantCulture);
        response.Headers[ActiveIncidentListContract.Headers.ServerTiming] =
            $"{QueryTimingMetricName};dur="
            + $"{queryExecutionMilliseconds.ToString("F2", CultureInfo.InvariantCulture)}, "
            + $"{DataAccessTimingMetricName};dur="
            + dataAccessMilliseconds.ToString("F2", CultureInfo.InvariantCulture);
    }
}
