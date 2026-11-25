using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Http;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents;

internal static class ListActiveIncidentSummariesEndpoint
{
    public static IEndpointRouteBuilder MapActiveIncidentSummaries(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ActiveIncidentSummaryContract.Route, ExecuteAsync)
            .WithName("ListActiveIncidentSummaries")
            .WithSummary("Compares full entity loading with an eight-field projection.");

        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<IncidentSummaryItem>>, ValidationProblem>> ExecuteAsync(
        [Description(ActiveIncidentSummaryContract.StrategyParameterDescription)] string? strategy,
        [Description(ActiveIncidentSummaryContract.LimitParameterDescription)] int? limit,
        OpsBoardDbContext dbContext,
        DbCommandMetrics commandMetrics,
        IncidentSummaryMetricsInterceptor shapeInterceptor,
        TimeProvider timeProvider,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var selectedLimit = limit ?? ActiveIncidentSummaryContract.DefaultLimit;
        var selectedStrategy = string.IsNullOrWhiteSpace(strategy)
            ? ActiveIncidentSummaryContract.Strategies.Projected
            : strategy.ToLowerInvariant();

        if (!ActiveIncidentSummaryContract.SupportedLimits.Contains(selectedLimit))
        {
            errors[nameof(limit)] =
                [$"The supported limits are: {string.Join(", ", ActiveIncidentSummaryContract.SupportedLimits)}."];
        }

        if (selectedStrategy is not (ActiveIncidentSummaryContract.Strategies.FullEntity
            or ActiveIncidentSummaryContract.Strategies.Projected))
        {
            errors[nameof(strategy)] = ["The supported strategies are: full-entity and projected."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var shape = shapeInterceptor.Start(dbContext);
        try
        {
            commandMetrics.Reset();
            var startedAt = timeProvider.GetTimestamp();
            var items = selectedStrategy == ActiveIncidentSummaryContract.Strategies.FullEntity
                ? await ActiveIncidentSummaryQuery.LoadFullEntitiesAsync(dbContext, selectedLimit, cancellationToken)
                : await ActiveIncidentSummaryQuery.CreateProjectedQuery(dbContext, selectedLimit)
                    .ToListAsync(cancellationToken);
            var elapsed = timeProvider.GetElapsedTime(startedAt);

            if (shape.RowCount is not int rowCount || shape.ColumnCount is not int columnCount)
            {
                throw new InvalidOperationException("The incident summary query did not yield reader metrics.");
            }

            IncidentMetricsResponse.Write(response, selectedStrategy, commandMetrics, elapsed);
            response.Headers[ActiveIncidentSummaryContract.Headers.RowCount] =
                rowCount.ToString(CultureInfo.InvariantCulture);
            response.Headers[ActiveIncidentSummaryContract.Headers.ColumnCount] =
                columnCount.ToString(CultureInfo.InvariantCulture);
            response.Headers[ActiveIncidentSummaryContract.Headers.MaterializedEntityCount] =
                shape.MaterializedEntityCount.ToString(CultureInfo.InvariantCulture);

            return TypedResults.Ok(items);
        }
        finally
        {
            shapeInterceptor.Stop(dbContext);
        }
    }
}
