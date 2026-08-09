using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents.Queries;

public sealed class IncidentListItemLoader(OpsBoardDbContext dbContext)
{
    internal async Task<IncidentListItem> LoadAsync(
        IncidentListSource incident,
        CancellationToken cancellationToken)
    {
        var ownership = await dbContext.Services
            .TagWith("Incident list: load service and team for one incident")
            .Where(service => service.Id == incident.ServiceId)
            .Select(service => new
            {
                Service = service.Name,
                Team = service.Team.Name
            })
            .SingleAsync(cancellationToken);

        var assignee = incident.AssigneeId is null
            ? null
            : await dbContext.Users
                .TagWith("Incident list: load assignee for one incident")
                .Where(user => user.Id == incident.AssigneeId)
                .Select(user => user.DisplayName)
                .SingleAsync(cancellationToken);

        var tags = await dbContext.IncidentTags
            .TagWith("Incident list: load tags for one incident")
            .Where(incidentTag => incidentTag.IncidentId == incident.Id)
            .OrderBy(incidentTag => incidentTag.Tag.Name)
            .Select(incidentTag => incidentTag.Tag.Name)
            .ToArrayAsync(cancellationToken);

        var updateCount = await dbContext.IncidentUpdates
            .TagWith("Incident list: count updates for one incident")
            .CountAsync(
                update => update.IncidentId == incident.Id,
                cancellationToken);

        return new IncidentListItem(
            incident.Number,
            incident.Title,
            incident.Severity,
            ownership.Service,
            ownership.Team,
            assignee,
            tags,
            updateCount,
            incident.LastActivityAt);
    }
}
