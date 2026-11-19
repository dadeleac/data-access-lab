using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Http;

namespace OpsBoard.Api.Incidents;

public static class ListActiveIncidentDetailsEndpoint
{
    public static IEndpointRouteBuilder MapActiveIncidentDetails(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                ActiveIncidentDetailsContract.Route,
                ExecuteAsync)
            .WithName("ListActiveIncidentDetails")
            .WithSummary("Loads active incident details with a single or split query.")
            .WithDescription(
                "Compares AsSingleQuery and AsSplitQuery for the same Include graph. "
                + "The strategy is part of the laboratory, not a production API contract.");

        return endpoints;
    }

    private static async Task<Results<
        Ok<IReadOnlyList<IncidentDetailsItem>>,
        ValidationProblem>> ExecuteAsync(
        [Description(ActiveIncidentDetailsContract.StrategyParameterDescription)]
        string? strategy,
        [Description(ActiveIncidentDetailsContract.LimitParameterDescription)]
        int? limit,
        SingleQueryIncidentDetailsQuery singleQuery,
        SplitQueryIncidentDetailsQuery splitQuery,
        DbCommandMetrics commandMetrics,
        TimeProvider timeProvider,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (!IncidentDetailsRequest.TryCreate(
                strategy,
                limit,
                out var request,
                out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var query = IncidentDetailsQuerySelector.Select(
            request!.Strategy,
            singleQuery,
            splitQuery);

        var strategyName = request.Strategy switch
        {
            IncidentDetailsStrategy.SingleQuery => ActiveIncidentDetailsContract.Strategies.SingleQuery,
            IncidentDetailsStrategy.SplitQuery => ActiveIncidentDetailsContract.Strategies.SplitQuery,
            _ => throw new UnreachableException()
        };

        commandMetrics.Reset();
        var startedAt = timeProvider.GetTimestamp();
        var items = await query.ExecuteAsync(request.Limit, cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        IncidentMetricsResponse.Write(response, strategyName, commandMetrics, elapsed);
        return TypedResults.Ok(items);
    }
}
