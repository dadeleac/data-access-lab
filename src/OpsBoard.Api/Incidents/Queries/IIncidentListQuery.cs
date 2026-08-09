namespace OpsBoard.Api.Incidents.Queries;

public interface IIncidentListQuery
{
    Task<IReadOnlyList<IncidentListItem>> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken);
}
