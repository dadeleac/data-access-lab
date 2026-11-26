namespace OpsBoard.Contracts.Configuration;

public sealed record DatasetOptions
{
    public const string ConfigurationSectionName = "Dataset";
    public const string SupportedProfile = "small";
    public const string LargeProfile = "large";
    public const int SmallUpdateCount = 4_000;
    public const int LargeUpdateCount = 50_000;

    public DatasetOptions()
    {
    }

    public DatasetOptions(
        string profile,
        int seed,
        DateTimeOffset referenceTime,
        string modelVersion)
    {
        Profile = profile;
        Seed = seed;
        ReferenceTime = referenceTime;
        ModelVersion = modelVersion;
    }

    public string Profile { get; set; } = string.Empty;
    public int Seed { get; set; }
    public DateTimeOffset ReferenceTime { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public bool Persist { get; set; } = true;

    public int ExpectedUpdateCount => Profile == LargeProfile ? LargeUpdateCount : SmallUpdateCount;

    public bool IsReferenceDataset => Profile == SupportedProfile
        && Seed == 1729
        && ReferenceTime == new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        && ModelVersion == "1";

    public void Validate()
    {
        if (Profile is not (SupportedProfile or LargeProfile))
        {
            throw new InvalidOperationException(
                $"Dataset profile '{Profile}' is not available. "
                + $"Supported profiles are '{SupportedProfile}' and '{LargeProfile}'.");
        }

        if (ReferenceTime.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"{ConfigurationSectionName}:{nameof(ReferenceTime)} must use UTC.");
        }

        if (ReferenceTime == default)
        {
            throw new InvalidOperationException(
                $"{ConfigurationSectionName}:{nameof(ReferenceTime)} is required.");
        }

        if (Seed == default)
        {
            throw new InvalidOperationException(
                $"{ConfigurationSectionName}:{nameof(Seed)} must be a non-zero integer.");
        }

        if (string.IsNullOrWhiteSpace(ModelVersion))
        {
            throw new InvalidOperationException(
                $"{ConfigurationSectionName}:{nameof(ModelVersion)} is required.");
        }
    }
}

public static class DatasetEnvironmentVariables
{
    private const string Separator = "__";

    public const string Profile =
        DatasetOptions.ConfigurationSectionName + Separator + nameof(DatasetOptions.Profile);
    public const string Seed =
        DatasetOptions.ConfigurationSectionName + Separator + nameof(DatasetOptions.Seed);
    public const string ReferenceTime =
        DatasetOptions.ConfigurationSectionName + Separator + nameof(DatasetOptions.ReferenceTime);
    public const string ModelVersion =
        DatasetOptions.ConfigurationSectionName + Separator + nameof(DatasetOptions.ModelVersion);
}
