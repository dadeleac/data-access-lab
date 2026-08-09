using System.Globalization;
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
    .WithImageTag(OpsBoardResourceNames.PostgresImageTag)
    .WithDataVolume(OpsBoardResourceNames.GetDatasetVolume(datasetOptions.Profile));

var database = postgres.AddDatabase(OpsBoardResourceNames.Database);

var dataGenerator = builder
    .AddProject<Projects.OpsBoard_DataGenerator>(OpsBoardResourceNames.DataGenerator)
    .WithReference(database)
    .WithEnvironment(DatasetEnvironmentVariables.Profile, datasetOptions.Profile)
    .WithEnvironment(
        DatasetEnvironmentVariables.Seed,
        datasetOptions.Seed.ToString(CultureInfo.InvariantCulture))
    .WithEnvironment(
        DatasetEnvironmentVariables.ReferenceTime,
        datasetOptions.ReferenceTime.ToString("O", CultureInfo.InvariantCulture))
    .WithEnvironment(DatasetEnvironmentVariables.ModelVersion, datasetOptions.ModelVersion)
    .WaitFor(database);

var api = builder.AddProject<Projects.OpsBoard_Api>(OpsBoardResourceNames.Api)
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(dataGenerator);

if (scenarioOptions.Run)
{
    builder.AddProject<Projects.OpsBoard_ScenarioRunner>(
            OpsBoardResourceNames.ScenarioRunner)
        .WithReference(api)
        .WaitFor(api);
}

builder.Build().Run();
