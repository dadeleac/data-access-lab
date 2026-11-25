using System.Globalization;

namespace OpsBoard.Contracts.Http;

public static class ActiveIncidentSummaryContract
{
    public const string Route = "/api/incidents/active/summary";
    public const int DefaultLimit = ActiveIncidentListContract.DefaultLimit;
    public const int FieldCount = 8;
    public const string StrategyParameterDescription =
        "Query strategy: full-entity or projected. Defaults to projected.";
    public const string LimitParameterDescription = ActiveIncidentListContract.LimitParameterDescription;

    public static IReadOnlyList<int> SupportedLimits => ActiveIncidentListContract.SupportedLimits;

    public static string CreateRequestPath(string strategy, int limit) =>
        $"{Route}?strategy={Uri.EscapeDataString(strategy)}&limit={limit.ToString(CultureInfo.InvariantCulture)}";

    public static class Strategies
    {
        public const string FullEntity = "full-entity";
        public const string Projected = "projected";
    }

    public static class Headers
    {
        public const string RowCount = "X-OpsBoard-Db-Rows";
        public const string ColumnCount = "X-OpsBoard-Db-Columns";
        public const string MaterializedEntityCount = "X-OpsBoard-Materialized-Entities";
    }
}
