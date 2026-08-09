using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents.Queries;

public sealed class BatchedIncidentListQuery(OpsBoardDbContext dbContext)
    : IIncidentListQuery
{
    public async Task<IReadOnlyList<IncidentListItem>> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var incidents = await ActiveIncidentListQuery
            .CreateSource(dbContext, limit)
            .TagWith("Incident list batch: active incidents")
            .ToListAsync(cancellationToken);

        if (incidents.Count == 0)
        {
            return [];
        }

        var incidentIds = incidents
            .Select(incident => incident.Id)
            .ToArray();
        var serviceIds = incidents
            .Select(incident => incident.ServiceId)
            .Distinct()
            .ToArray();
        var assigneeIds = incidents
            .Where(incident => incident.AssigneeId is not null)
            .Select(incident => incident.AssigneeId!.Value)
            .Distinct()
            .ToArray();

        var ownershipByServiceId = await dbContext.Services
            .TagWith("Incident list batch: services and teams")
            .AsNoTracking()
            .Where(service => serviceIds.Contains(service.Id))
            .Select(service => new
            {
                service.Id,
                Service = service.Name,
                Team = service.Team.Name
            })
            .ToDictionaryAsync(row => row.Id, cancellationToken);

        var assigneeById = await dbContext.Users
            .TagWith("Incident list batch: assignees")
            .AsNoTracking()
            .Where(user => assigneeIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.DisplayName,
                cancellationToken);

        var tagRows = await dbContext.IncidentTags
            .TagWith("Incident list batch: tags")
            .AsNoTracking()
            .Where(incidentTag => incidentIds.Contains(incidentTag.IncidentId))
            .OrderBy(incidentTag => incidentTag.Tag.Name)
            .Select(incidentTag => new
            {
                incidentTag.IncidentId,
                incidentTag.Tag.Name
            })
            .ToListAsync(cancellationToken);

        var tagsByIncidentId = tagRows
            .GroupBy(row => row.IncidentId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => row.Name).ToArray());

        var updateCountByIncidentId = await dbContext.IncidentUpdates
            .TagWith("Incident list batch: update counts")
            .AsNoTracking()
            .Where(update => incidentIds.Contains(update.IncidentId))
            .GroupBy(update => update.IncidentId)
            .Select(group => new
            {
                IncidentId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                row => row.IncidentId,
                row => row.Count,
                cancellationToken);

        return incidents
            .Select(incident =>
            {
                var ownership = ownershipByServiceId[incident.ServiceId];
                var assignee = incident.AssigneeId is { } assigneeId
                    ? assigneeById[assigneeId]
                    : null;
                var tags = tagsByIncidentId.GetValueOrDefault(incident.Id, []);
                var updateCount = updateCountByIncidentId.GetValueOrDefault(incident.Id);

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
            })
            .ToArray();
    }
}
