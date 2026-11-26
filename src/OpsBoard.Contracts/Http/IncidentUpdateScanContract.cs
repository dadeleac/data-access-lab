using System.Globalization;
using OpsBoard.Contracts.Configuration;

namespace OpsBoard.Contracts.Http;

public static class IncidentUpdateScanContract
{
    public const string Route = "/api/incident-updates/scan";
    public const int DefaultLimit = 1_000;
    public const int MaximumLimit = DatasetOptions.LargeUpdateCount;
    public const string MeasureMemoryConfigurationKey = "Lab04:MeasureMemory";

    public static IReadOnlyList<string> SupportedStrategies { get; } = Array.AsReadOnly(
        [Strategies.Tracking, Strategies.NoTracking, Strategies.IdentityResolution, Strategies.Buffered]);

    public static string CreateRequestPath(string strategy, int limit) =>
        $"{Route}?strategy={Uri.EscapeDataString(strategy)}&limit={limit.ToString(CultureInfo.InvariantCulture)}";

    public static class Strategies
    {
        public const string Tracking = "tracking";
        public const string NoTracking = "no-tracking";
        public const string IdentityResolution = "identity-resolution";
        public const string Buffered = "buffered";
    }

    public static class Headers
    {
        public const string TrackedEntries = "X-OpsBoard-Tracked-Entries";
        public const string AllocatedBytes = "X-OpsBoard-Allocated-Bytes";
        public const string BaselineManagedBytes = "X-OpsBoard-Baseline-Managed-Bytes";
        public const string PeakManagedBytes = "X-OpsBoard-Peak-Managed-Bytes";
    }
}

public sealed record IncidentUpdateScanResult(int ItemCount, int DistinctAuthorCount, string Checksum);
