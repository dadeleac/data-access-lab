using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Contracts.Http;

namespace OpsBoard.IntegrationTests.Api;

public sealed class IncidentUpdateScanQueryTests
{
    [Fact]
    public void All_strategies_translate_to_the_same_sql()
    {
        using var dbContext = TestDbContextFactory.Create();
        var queries = IncidentUpdateScanContract.SupportedStrategies
            .Select(strategy => IncidentUpdateScanQuery.Create(dbContext, strategy, 1_000).ToQueryString())
            .ToArray();

        Assert.Single(queries.Distinct());
        Assert.Contains("JOIN \"Users\"", queries[0], StringComparison.Ordinal);
        Assert.Contains("ORDER BY", queries[0], StringComparison.Ordinal);
    }
}
