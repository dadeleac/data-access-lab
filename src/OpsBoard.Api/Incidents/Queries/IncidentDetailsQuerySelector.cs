using OpsBoard.Api.Incidents;

namespace OpsBoard.Api.Incidents.Queries;

internal static class IncidentDetailsQuerySelector
{
    public static IIncidentDetailsQuery Select(
        IncidentDetailsStrategy strategy,
        SingleQueryIncidentDetailsQuery singleQuery,
        SplitQueryIncidentDetailsQuery splitQuery) =>
        strategy switch
        {
            IncidentDetailsStrategy.SingleQuery => singleQuery,
            IncidentDetailsStrategy.SplitQuery => splitQuery,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
        };
}
