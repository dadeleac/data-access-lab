using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Http;

namespace OpsBoard.Api.Incidents;

public static class ListActiveIncidentsEndpoint
{
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

        (IIncidentListQuery Query, string StrategyName) selected = request!.Strategy switch
        {
            IncidentListStrategy.PerIncident =>
                (perIncidentQuery, ActiveIncidentListContract.Strategies.PerIncident),
            IncidentListStrategy.Batched =>
                (batchedQuery, ActiveIncidentListContract.Strategies.Batched),
            IncidentListStrategy.Projected =>
                (projectedQuery, ActiveIncidentListContract.Strategies.Projected),
            _ => throw new UnreachableException()
        };

        commandMetrics.Reset();
        var startedAt = timeProvider.GetTimestamp();
        var items = await selected.Query.ExecuteAsync(request.Limit, cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        IncidentMetricsResponse.Write(response, selected.StrategyName, commandMetrics, elapsed);
        return TypedResults.Ok(items);
    }
}
