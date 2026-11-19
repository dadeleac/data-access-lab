using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Incidents;
using OpsBoard.Data;

namespace OpsBoard.Api.Incidents.Queries;

public sealed class SplitQueryIncidentDetailsQuery(OpsBoardDbContext dbContext)
    : IIncidentDetailsQuery
{
    public async Task<IReadOnlyList<IncidentDetailsItem>> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var incidents = await ActiveIncidentDetailsQuery
            .Create(dbContext, limit)
            .TagWith("Incident details: active incidents")
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return ActiveIncidentDetailsMapper.Map(incidents);
    }
}
