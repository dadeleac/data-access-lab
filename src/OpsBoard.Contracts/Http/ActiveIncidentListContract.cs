using System.Collections.ObjectModel;
using System.Globalization;

namespace OpsBoard.Contracts.Http;

public static class ActiveIncidentListContract
{
    public const string Route = "/api/incidents/active";
    public const string StrategyQueryParameter = "strategy";
    public const string LimitQueryParameter = "limit";
    public const int DefaultLimit = 50;
    public const string SupportedStrategiesDisplay =
        "per-incident, batched and projected";
    public const string StrategyParameterDescription =
        "Data-access strategy: per-incident, batched or projected. "
        + "Defaults to per-incident.";
    public const string LimitParameterDescription =
        "Maximum number of active incidents. Supported values: 10, 25, 50 and 100. "
        + "Defaults to 50.";

    public static ReadOnlyCollection<int> SupportedLimits { get; } =
        Array.AsReadOnly([10, 25, 50, 100]);

    public static string CreateRequestPath(string strategy, int limit) =>
        $"{Route}?{StrategyQueryParameter}={Uri.EscapeDataString(strategy)}"
        + $"&{LimitQueryParameter}={limit.ToString(CultureInfo.InvariantCulture)}";

    public static class Strategies
    {
        public const string PerIncident = "per-incident";
        public const string Batched = "batched";
        public const string Projected = "projected";
    }

    public static class Headers
    {
        public const string Strategy = "X-OpsBoard-Strategy";
        public const string QueryCount = "X-OpsBoard-Db-Queries";
        public const string QueryDuration = "X-OpsBoard-Db-Query-Duration-Ms";
        public const string DataAccessElapsed = "X-OpsBoard-Data-Access-Elapsed-Ms";
        public const string ServerTiming = "Server-Timing";
    }
}
