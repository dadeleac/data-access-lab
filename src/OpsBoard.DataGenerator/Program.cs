using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Contracts.Hosting;
using OpsBoard.Data;
using OpsBoard.DataGenerator.Generation;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
var datasetOptions = builder.Configuration
    .GetSection(DatasetOptions.ConfigurationSectionName)
    .Get<DatasetOptions>()
    ?? new DatasetOptions();
datasetOptions.Validate();

var connectionString = builder.Configuration
    .GetConnectionString(OpsBoardResourceNames.Database)
    ?? throw new InvalidOperationException(
        $"The '{OpsBoardResourceNames.Database}' connection string is required.");

builder.Services.AddDbContext<OpsBoardDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddSingleton<SmallDatasetFactory>();
builder.Services.AddScoped<DatasetInitializer>();

using var host = builder.Build();
await host.StartAsync();

await using var scope = host.Services.CreateAsyncScope();
var initializer = scope.ServiceProvider.GetRequiredService<DatasetInitializer>();
await initializer.InitializeAsync(datasetOptions);

await host.StopAsync();
