using System.Collections.ObjectModel;
using System.Globalization;

namespace OpsBoard.Contracts.Http;

public static class ActiveIncidentDetailsContract
{
    public const string Route = "/api/incidents/active/details";
    public const string StrategyQueryParameter = "strategy";
    public const string LimitQueryParameter = "limit";
    public const int DefaultLimit = 50;
    public const string SupportedStrategiesDisplay = "single-query and split-query";
    public const string StrategyParameterDescription =
        "Query strategy: single-query or split-query. Defaults to single-query.";
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
        public const string SingleQuery = "single-query";
        public const string SplitQuery = "split-query";
    }
}
