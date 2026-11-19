using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Contracts.Hosting;

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

if (scenarioOptions.Run)
{
    builder.AddDotnetProject(
            OpsBoardResourceNames.ScenarioRunner,
            "../OpsBoard.ScenarioRunner/OpsBoard.ScenarioRunner.csproj")
        .WithReference(api)
        .WithEnvironment(ConfigureDataset)
        .WaitFor(api);
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
