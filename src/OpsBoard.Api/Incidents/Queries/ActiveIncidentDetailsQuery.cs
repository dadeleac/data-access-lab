using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;
using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents.Queries;

internal static class ActiveIncidentDetailsQuery
{
    public static IQueryable<Incident> Create(
        OpsBoardDbContext dbContext,
        int limit) =>
        dbContext.Incidents
            .AsNoTracking()
            .Where(incident =>
                incident.Status == IncidentStatus.Open
                || incident.Status == IncidentStatus.Investigating)
            .OrderByDescending(incident => incident.LastActivityAt)
            .ThenByDescending(incident => incident.Id)
            .Take(limit)
            .Include(incident => incident.Service)
                .ThenInclude(service => service.Team)
            .Include(incident => incident.Assignee)
            .Include(incident => incident.IncidentTags)
                .ThenInclude(incidentTag => incidentTag.Tag)
            .Include(incident => incident.Updates)
                .ThenInclude(update => update.Author);
}
