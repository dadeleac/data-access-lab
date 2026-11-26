using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Contracts.Hosting;
using OpsBoard.Contracts.Http;

var builder = DistributedApplication.CreateBuilder(args);

var datasetOptions = builder.Configuration
    .GetSection(DatasetOptions.ConfigurationSectionName)
    .Get<DatasetOptions>()
    ?? new DatasetOptions();
datasetOptions.Validate();

var scenarioOptions = builder.Configuration
    .GetSection(ScenarioOptions.ConfigurationSectionName)
    .Get<ScenarioOptions>()
    ?? new ScenarioOptions();

var measuredStrategy = scenarioOptions.Lab04Strategy;
if (measuredStrategy is not null
    && (!scenarioOptions.Run || !IncidentUpdateScanContract.SupportedStrategies.Contains(measuredStrategy)))
{
    throw new InvalidOperationException("Scenarios:Lab04Strategy requires Scenarios:Run and a supported strategy.");
}

var postgres = builder.AddPostgres(OpsBoardResourceNames.Postgres)
    .WithImageTag(OpsBoardResourceNames.PostgresImageTag);

if (datasetOptions.Persist)
{
    postgres.WithDataVolume(OpsBoardResourceNames.GetDatasetVolume(datasetOptions.Profile));
}

var database = postgres.AddDatabase(OpsBoardResourceNames.Database);

var dataGenerator = builder
    .AddDotnetProject(
        OpsBoardResourceNames.DataGenerator,
        "../OpsBoard.DataGenerator/OpsBoard.DataGenerator.csproj")
    .WithReference(database)
    .WithEnvironment(ConfigureDataset)
    .WaitFor(database);

var api = builder
    .AddDotnetProject(OpsBoardResourceNames.Api, "../OpsBoard.Api/OpsBoard.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(dataGenerator);

if (measuredStrategy is not null)
{
    api.WithEnvironment(
        IncidentUpdateScanContract.MeasureMemoryConfigurationKey.Replace(":", "__", StringComparison.Ordinal), "true");
}

if (scenarioOptions.Run)
{
    var scenarioRunner = builder.AddDotnetProject(
            OpsBoardResourceNames.ScenarioRunner,
            "../OpsBoard.ScenarioRunner/OpsBoard.ScenarioRunner.csproj")
        .WithReference(api)
        .WithEnvironment(ConfigureDataset)
        .WaitFor(api);

    if (measuredStrategy is not null)
    {
        scenarioRunner.WithEnvironment(
            $"{ScenarioOptions.ConfigurationSectionName}__{nameof(ScenarioOptions.Lab04Strategy)}", measuredStrategy);
    }
}

builder.Build().Run();

void ConfigureDataset(EnvironmentCallbackContext context)
{
    context.EnvironmentVariables[DatasetEnvironmentVariables.Profile] = datasetOptions.Profile;
    context.EnvironmentVariables[DatasetEnvironmentVariables.Seed] =
        datasetOptions.Seed.ToString(CultureInfo.InvariantCulture);
    context.EnvironmentVariables[DatasetEnvironmentVariables.ReferenceTime] =
        datasetOptions.ReferenceTime.ToString("O", CultureInfo.InvariantCulture);
    context.EnvironmentVariables[DatasetEnvironmentVariables.ModelVersion] = datasetOptions.ModelVersion;
}
