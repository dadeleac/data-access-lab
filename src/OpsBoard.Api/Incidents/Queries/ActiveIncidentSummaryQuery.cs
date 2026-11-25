using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;
using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents.Queries;

internal static class ActiveIncidentSummaryQuery
{
    private static readonly Expression<Func<Incident, IncidentSummaryItem>> Projection =
        incident => new IncidentSummaryItem(
            incident.Number,
            incident.Title,
            incident.Status,
            incident.Severity,
            incident.Service.Name,
            incident.Service.Team.Name,
            incident.Assignee == null ? null : incident.Assignee.DisplayName,
            incident.LastActivityAt);

    private static readonly Func<Incident, IncidentSummaryItem> Map = Projection.Compile();

    public static IQueryable<Incident> CreateFullEntityQuery(OpsBoardDbContext dbContext, int limit) =>
        ActiveIncidentListQuery.Create(dbContext, limit)
            .TagWith("Lab 03: materialize full incident and reference entities")
            .Include(incident => incident.Service)
                .ThenInclude(service => service.Team)
            .Include(incident => incident.Assignee);

    public static IQueryable<IncidentSummaryItem> CreateProjectedQuery(OpsBoardDbContext dbContext, int limit) =>
        ActiveIncidentListQuery.Create(dbContext, limit)
            .TagWith("Lab 03: project eight incident summary fields")
            .Select(Projection);

    public static async Task<IReadOnlyList<IncidentSummaryItem>> LoadFullEntitiesAsync(
        OpsBoardDbContext dbContext,
        int limit,
        CancellationToken cancellationToken)
    {
        var incidents = await CreateFullEntityQuery(dbContext, limit).ToListAsync(cancellationToken);
        return incidents.Select(Map).ToArray();
    }
}
