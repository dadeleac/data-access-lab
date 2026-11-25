using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Incidents.Queries;

namespace OpsBoard.IntegrationTests.Api;

public sealed class ActiveIncidentSummaryQueryTests
{
    [Fact]
    public void Full_entities_select_unused_columns_that_the_projection_omits()
    {
        using var dbContext = TestDbContextFactory.Create();

        var full = ActiveIncidentSummaryQuery.CreateFullEntityQuery(dbContext, 50).ToQueryString();
        var projected = ActiveIncidentSummaryQuery.CreateProjectedQuery(dbContext, 50).ToQueryString();

        foreach (var unusedColumn in new[] { "\"Description\"", "\"SlaDueAt\"", "\"Slug\"", "\"Email\"" })
        {
            Assert.Contains(unusedColumn, full, StringComparison.Ordinal);
            Assert.DoesNotContain(unusedColumn, projected, StringComparison.Ordinal);
        }

        foreach (var sql in new[] { full, projected })
        {
            Assert.Contains("LEFT JOIN \"Users\"", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("\"IncidentTags\"", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("\"IncidentUpdates\"", sql, StringComparison.Ordinal);
        }
    }
}
