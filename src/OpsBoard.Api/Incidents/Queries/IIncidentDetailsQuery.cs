using OpsBoard.Api.Incidents;

namespace OpsBoard.Api.Incidents.Queries;

internal interface IIncidentDetailsQuery
{
    Task<IReadOnlyList<IncidentDetailsItem>> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken);
}
