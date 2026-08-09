using OpsBoard.Api.Incidents.Queries;
using Microsoft.EntityFrameworkCore;

namespace OpsBoard.IntegrationTests.Api;

public sealed class ActiveIncidentListQueryTests
{
    [Fact]
    public void Source_query_selects_only_the_incident_columns_used_by_the_list()
    {
        using var dbContext = TestDbContextFactory.Create();

        var sql = ActiveIncidentListQuery
            .CreateSource(dbContext, 50)
            .ToQueryString();

        Assert.Contains("\"Id\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"ServiceId\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"AssigneeId\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Number\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Title\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Severity\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"LastActivityAt\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Description\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"CreatedAt\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"SlaDueAt\"", sql, StringComparison.Ordinal);
    }
}
