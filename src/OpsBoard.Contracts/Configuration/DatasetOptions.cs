namespace OpsBoard.Contracts.Configuration;

public sealed record DatasetOptions
{
    public const string ConfigurationSectionName = "Dataset";
    public const string SupportedProfile = "small";

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

    public void Validate()
    {
        if (!string.Equals(Profile, SupportedProfile, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Dataset profile '{Profile}' is not available. "
                + $"The current iteration supports only '{SupportedProfile}'.");
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
