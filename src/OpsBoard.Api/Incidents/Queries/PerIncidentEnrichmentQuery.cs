using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents.Queries;

public sealed class PerIncidentEnrichmentQuery(
    OpsBoardDbContext dbContext,
    IncidentListItemLoader itemLoader)
    : IIncidentListQuery
{
    public async Task<IReadOnlyList<IncidentListItem>> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var incidents = await ActiveIncidentListQuery
            .CreateSource(dbContext, limit)
            .TagWith("Incident list per incident: active incidents")
            .ToListAsync(cancellationToken);

        var items = new List<IncidentListItem>(incidents.Count);

        foreach (var incident in incidents)
        {
            items.Add(await itemLoader.LoadAsync(incident, cancellationToken));
        }

        return items;
    }
}
