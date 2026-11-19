using OpsBoard.Api.Incidents;
using OpsBoard.Api.Incidents.Queries;
using OpsBoard.Data.Model;
using System.Text.Json;

namespace OpsBoard.IntegrationTests.Api;

public sealed class IncidentDetailsQueryTests
{
    [Fact]
    public void Selector_returns_the_requested_query_only()
    {
        using var dbContext = TestDbContextFactory.Create();
        var single = new SingleQueryIncidentDetailsQuery(dbContext);
        var split = new SplitQueryIncidentDetailsQuery(dbContext);

        Assert.Same(
            single,
            IncidentDetailsQuerySelector.Select(
                IncidentDetailsStrategy.SingleQuery,
                single,
                split));
        Assert.Same(
            split,
            IncidentDetailsQuerySelector.Select(
                IncidentDetailsStrategy.SplitQuery,
                single,
                split));
    }

    [Fact]
    public void Mapper_is_shared_deterministic_and_orders_nested_collections()
    {
        var team = new Team { Id = 1, Name = "Platform", Slug = "platform" };
        var service = new Service
        {
            Id = 2,
            Name = "API",
            Slug = "api",
            Team = team
        };
        var author = new User
        {
            Id = 3,
            DisplayName = "Operator",
            Email = "operator@example.test"
        };
        var incident = new Incident
        {
            Id = 4,
            Number = "INC-000004",
            Title = "Title",
            Description = "Description",
            Severity = IncidentSeverity.High,
            Service = service,
            Assignee = author
        };

        incident.IncidentTags.Add(new IncidentTag
        {
            Incident = incident,
            Tag = new Tag { Id = 2, Name = "zeta", Slug = "zeta", Color = "#000000" }
        });
        incident.IncidentTags.Add(new IncidentTag
        {
            Incident = incident,
            Tag = new Tag { Id = 1, Name = "alpha", Slug = "alpha", Color = "#000000" }
        });
        incident.Updates.Add(new IncidentUpdate
        {
            Id = 2,
            Incident = incident,
            Author = author,
            Kind = IncidentUpdateKind.Comment,
            Body = "later",
            CreatedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        });
        incident.Updates.Add(new IncidentUpdate
        {
            Id = 1,
            Incident = incident,
            Author = author,
            Kind = IncidentUpdateKind.Investigation,
            Body = "earlier id",
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        var mapped = ActiveIncidentDetailsMapper.Map([incident]);
        var mappedAgain = ActiveIncidentDetailsMapper.Map([incident]);

        Assert.Equal(
            JsonSerializer.Serialize(mapped),
            JsonSerializer.Serialize(mappedAgain));
        Assert.Equal(["alpha", "zeta"], mapped[0].Tags);
        Assert.Equal(["earlier id", "later"], mapped[0].Updates.Select(update => update.Body));
        Assert.Equal("Platform", mapped[0].Team);
        Assert.Equal("Operator", mapped[0].Assignee);
    }
}
