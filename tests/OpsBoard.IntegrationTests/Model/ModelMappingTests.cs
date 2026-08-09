using OpsBoard.Data.Model;

namespace OpsBoard.IntegrationTests.Model;

public sealed class ModelMappingTests
{
    [Fact]
    public void Model_contains_the_expected_initial_entities()
    {
        using var dbContext = TestDbContextFactory.Create();

        Assert.NotNull(dbContext.Model.FindEntityType(typeof(Incident)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(IncidentUpdate)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(IncidentPause)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(IncidentStatusChange)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(DatasetMetadata)));
    }

    [Fact]
    public void Incident_tag_uses_a_composite_primary_key()
    {
        using var dbContext = TestDbContextFactory.Create();

        var entityType = dbContext.Model.FindEntityType(typeof(IncidentTag));
        var primaryKey = entityType?.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Collection(
            primaryKey.Properties,
            property => Assert.Equal(nameof(IncidentTag.IncidentId), property.Name),
            property => Assert.Equal(nameof(IncidentTag.TagId), property.Name));
    }
}
