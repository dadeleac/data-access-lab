using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;
using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents.Queries;

internal static class ActiveIncidentListQuery
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
            .Take(limit);

    public static IQueryable<IncidentListSource> CreateSource(
        OpsBoardDbContext dbContext,
        int limit) =>
        Create(dbContext, limit)
            .Select(incident => new IncidentListSource(
                incident.Id,
                incident.ServiceId,
                incident.AssigneeId,
                incident.Number,
                incident.Title,
                incident.Severity,
                incident.LastActivityAt));
}

internal sealed record IncidentListSource(
    long Id,
    long ServiceId,
    long? AssigneeId,
    string Number,
    string Title,
    IncidentSeverity Severity,
    DateTimeOffset LastActivityAt);
