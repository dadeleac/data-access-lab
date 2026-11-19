namespace OpsBoard.Contracts.Http;

public static class IncidentMetricsHeaders
{
    public const string Strategy = "X-OpsBoard-Strategy";
    public const string QueryCount = "X-OpsBoard-Db-Queries";
    public const string QueryDuration = "X-OpsBoard-Db-Query-Duration-Ms";
    public const string DataAccessElapsed = "X-OpsBoard-Data-Access-Elapsed-Ms";
    public const string ServerTiming = "Server-Timing";
}
