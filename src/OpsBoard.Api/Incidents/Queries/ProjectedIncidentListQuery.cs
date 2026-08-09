using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents.Queries;

public sealed class ProjectedIncidentListQuery(OpsBoardDbContext dbContext)
    : IIncidentListQuery
{
    public async Task<IReadOnlyList<IncidentListItem>> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var items = await ActiveIncidentListQuery
            .Create(dbContext, limit)
            .TagWith("Incident list projection")
            .Select(incident => new IncidentListItem(
                incident.Number,
                incident.Title,
                incident.Severity,
                incident.Service.Name,
                incident.Service.Team.Name,
                incident.Assignee == null
                    ? null
                    : incident.Assignee.DisplayName,
                incident.IncidentTags
                    .OrderBy(incidentTag => incidentTag.Tag.Name)
                    .Select(incidentTag => incidentTag.Tag.Name)
                    .ToArray(),
                incident.Updates.Count,
                incident.LastActivityAt))
            .ToListAsync(cancellationToken);

        return items;
    }
}
