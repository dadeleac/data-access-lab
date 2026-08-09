using OpsBoard.Contracts.Configuration;
using OpsBoard.Data.Model;
using OpsBoard.DataGenerator.Generation;

namespace OpsBoard.IntegrationTests.Generation;

public sealed class SmallDatasetFactoryTests
{
    private static readonly DatasetOptions Options = new(
        DatasetOptions.SupportedProfile,
        1729,
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        "1");

    [Fact]
    public void Small_profile_has_the_expected_size_and_shape()
    {
        var dataset = new SmallDatasetFactory().Build(Options);

        Assert.Equal(8, dataset.Teams.Count);
        Assert.Equal(30, dataset.Services.Count);
        Assert.Equal(150, dataset.Users.Count);
        Assert.Equal(24, dataset.Tags.Count);
        Assert.Equal(3, dataset.SlaPolicies.Count);
        Assert.Equal(12, dataset.SlaTargets.Count);
        Assert.Equal(500, dataset.Incidents.Count);
        Assert.Equal(1_100, dataset.IncidentTags.Count);
        Assert.Equal(4_000, dataset.Updates.Count);
        Assert.Equal(100, dataset.Pauses.Count);
        Assert.Equal(1_440, dataset.StatusChanges.Count);

        Assert.Equal(60, dataset.Incidents.Count(x => x.Status == IncidentStatus.Open));
        Assert.Equal(
            40,
            dataset.Incidents.Count(x => x.Status == IncidentStatus.Investigating));
        Assert.Equal(
            300,
            dataset.Incidents.Count(x => x.Status == IncidentStatus.Resolved));
        Assert.Equal(
            100,
            dataset.Incidents.Count(x => x.Status == IncidentStatus.Closed));

        var incidentsInBusiestServices = dataset.Services
            .Take(5)
            .Sum(service => service.Incidents.Count);
        Assert.Equal(300, incidentsInBusiestServices);
    }

    [Fact]
    public void Same_options_produce_the_same_fingerprint()
    {
        var factory = new SmallDatasetFactory();

        var first = DatasetFingerprint.Compute(factory.Build(Options));
        var second = DatasetFingerprint.Compute(factory.Build(Options));
        var differentSeed = DatasetFingerprint.Compute(
            factory.Build(Options with { Seed = Options.Seed + 1 }));

        Assert.Equal(first, second);
        Assert.NotEqual(first, differentSeed);
    }

    [Fact]
    public void Generated_incidents_preserve_temporal_and_relational_invariants()
    {
        var dataset = new SmallDatasetFactory().Build(Options);

        Assert.Equal(
            dataset.Incidents.Count,
            dataset.Incidents.Select(x => x.Number).Distinct().Count());

        foreach (var incident in dataset.Incidents)
        {
            Assert.InRange(
                incident.LastActivityAt,
                incident.CreatedAt,
                Options.ReferenceTime);
            Assert.True(incident.SlaDueAt > incident.CreatedAt);
            Assert.Equal(
                incident.IncidentTags.Count,
                incident.IncidentTags.Select(x => x.Tag.Slug).Distinct().Count());

            if (incident.Status is IncidentStatus.Resolved or IncidentStatus.Closed)
            {
                Assert.NotNull(incident.ResolvedAt);
            }
            else
            {
                Assert.Null(incident.ResolvedAt);
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                Assert.NotNull(incident.ClosedAt);
                Assert.True(incident.ClosedAt >= incident.ResolvedAt);
            }
            else
            {
                Assert.Null(incident.ClosedAt);
            }

            var terminalTime =
                incident.ClosedAt ?? incident.ResolvedAt ?? Options.ReferenceTime;

            Assert.All(
                incident.Updates,
                update => Assert.InRange(
                    update.CreatedAt,
                    incident.CreatedAt,
                    terminalTime));
            Assert.All(
                incident.Pauses,
                pause =>
                {
                    Assert.InRange(
                        pause.StartedAt,
                        incident.CreatedAt,
                        terminalTime);

                    if (pause.EndedAt is not null)
                    {
                        Assert.InRange(
                            pause.EndedAt.Value,
                            pause.StartedAt,
                            terminalTime);
                    }
                });
        }
    }
}
