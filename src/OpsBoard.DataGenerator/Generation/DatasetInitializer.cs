using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Data;
using OpsBoard.Data.Model;

namespace OpsBoard.DataGenerator.Generation;

public sealed class DatasetInitializer(
    OpsBoardDbContext dbContext,
    SmallDatasetFactory datasetFactory,
    TimeProvider timeProvider,
    ILogger<DatasetInitializer> logger)
{
    public async Task<DatasetInitializationResult> InitializeAsync(
        DatasetOptions options,
        CancellationToken cancellationToken = default)
    {
        options.Validate();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var metadata = await dbContext.DatasetMetadata
            .SingleOrDefaultAsync(cancellationToken);

        if (metadata is not null)
        {
            EnsureMetadataMatches(options, metadata);
            await EnsureStoredDatasetIsCompleteAsync(cancellationToken);

            logger.LogInformation(
                "Dataset {Profile} already available. Seed: {Seed}; reference time: {ReferenceTime}; model version: {ModelVersion}.",
                options.Profile,
                options.Seed,
                options.ReferenceTime,
                options.ModelVersion);

            return DatasetInitializationResult.Reused;
        }

        if (await ContainsDomainDataAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "The database contains OpsBoard data without DatasetMetadata. "
                + "Refusing to overwrite or extend an unknown dataset.");
        }

        logger.LogInformation(
            "Generating dataset {Profile}. Seed: {Seed}; reference time: {ReferenceTime}; model version: {ModelVersion}.",
            options.Profile,
            options.Seed,
            options.ReferenceTime,
            options.ModelVersion);

        var dataset = datasetFactory.Build(options);
        var fingerprint = DatasetFingerprint.Compute(dataset);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.AddRange(dataset.Teams);
        dbContext.AddRange(dataset.SlaPolicies);
        dbContext.AddRange(dataset.Users);
        dbContext.AddRange(dataset.Tags);
        dbContext.AddRange(dataset.Incidents);
        dbContext.DatasetMetadata.Add(new DatasetMetadata
        {
            Id = 1,
            Profile = options.Profile,
            Seed = options.Seed,
            ReferenceTime = options.ReferenceTime,
            GeneratedAt = timeProvider.GetUtcNow(),
            ModelVersion = options.ModelVersion
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Dataset {Profile} generated. Incidents: {IncidentCount}; updates: {UpdateCount}; pauses: {PauseCount}; status changes: {StatusChangeCount}; fingerprint: {Fingerprint}.",
            options.Profile,
            dataset.Incidents.Count,
            dataset.Updates.Count,
            dataset.Pauses.Count,
            dataset.StatusChanges.Count,
            fingerprint);

        return DatasetInitializationResult.Created;
    }

    private static void EnsureMetadataMatches(
        DatasetOptions requested,
        DatasetMetadata stored)
    {
        if (Matches(requested, stored))
        {
            return;
        }

        throw new InvalidOperationException(
            "The selected database volume contains a different dataset. "
            + $"Stored: profile={stored.Profile}, seed={stored.Seed}, "
            + $"reference-time={stored.ReferenceTime:O}, model-version={stored.ModelVersion}. "
            + $"Requested: profile={requested.Profile}, seed={requested.Seed}, "
            + $"reference-time={requested.ReferenceTime:O}, model-version={requested.ModelVersion}. "
            + "No data was changed.");
    }

    private static bool Matches(
        DatasetOptions requested,
        DatasetMetadata stored) =>
        string.Equals(requested.Profile, stored.Profile, StringComparison.Ordinal)
        && requested.Seed == stored.Seed
        && requested.ReferenceTime == stored.ReferenceTime
        && string.Equals(
            requested.ModelVersion,
            stored.ModelVersion,
            StringComparison.Ordinal);

    private async Task EnsureStoredDatasetIsCompleteAsync(
        CancellationToken cancellationToken)
    {
        var incidentCount = await dbContext.Incidents.CountAsync(cancellationToken);
        var updateCount = await dbContext.IncidentUpdates.CountAsync(cancellationToken);

        if (incidentCount != 500 || updateCount != 4_000)
        {
            throw new InvalidOperationException(
                "DatasetMetadata is present, but the stored small dataset is incomplete. "
                + $"Expected 500 incidents and 4000 updates; found {incidentCount} and {updateCount}. "
                + "No data was changed.");
        }
    }

    private async Task<bool> ContainsDomainDataAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Teams.AnyAsync(cancellationToken)
        || await dbContext.Users.AnyAsync(cancellationToken)
        || await dbContext.Incidents.AnyAsync(cancellationToken);
}

public enum DatasetInitializationResult
{
    Created,
    Reused
}
