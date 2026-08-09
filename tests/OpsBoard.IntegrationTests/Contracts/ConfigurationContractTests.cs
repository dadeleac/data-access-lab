using OpsBoard.Contracts.Configuration;
using OpsBoard.Contracts.Hosting;
using OpsBoard.Contracts.Http;

namespace OpsBoard.IntegrationTests.Contracts;

public sealed class ConfigurationContractTests
{
    [Fact]
    public void Missing_dataset_configuration_is_rejected()
    {
        var options = new DatasetOptions();

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("Dataset profile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_names_build_the_expected_integration_contract()
    {
        Assert.Equal(
            "ConnectionStrings__opsboard",
            OpsBoardResourceNames.DatabaseConnectionStringEnvironmentVariable);
        Assert.Equal(
            "opsboard-small-data",
            OpsBoardResourceNames.GetDatasetVolume(DatasetOptions.SupportedProfile));
        Assert.Equal(
            "/api/incidents/active?strategy=batched&limit=50",
            ActiveIncidentListContract.CreateRequestPath(
                ActiveIncidentListContract.Strategies.Batched,
                ActiveIncidentListContract.DefaultLimit));
        Assert.Equal("/scalar/v1", ApiDocumentationContract.ScalarRoute);
    }
}
