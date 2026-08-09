namespace OpsBoard.Contracts.Hosting;

public static class OpsBoardResourceNames
{
    private const string ConnectionStringsPrefix = "ConnectionStrings__";

    public const string Postgres = "postgres";
    public const string Database = "opsboard";
    public const string DataGenerator = "data-generator";
    public const string Api = "api";
    public const string ScenarioRunner = "scenario-runner";
    public const string PostgresImageTag = "18";
    public const string DatabaseConnectionStringEnvironmentVariable =
        ConnectionStringsPrefix + Database;
    public const string ApiServiceUri = "https+http://" + Api;

    public static string GetDatasetVolume(string profile) =>
        $"{Database}-{profile}-data";
}
