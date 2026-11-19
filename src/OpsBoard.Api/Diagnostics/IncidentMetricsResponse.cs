using System.Globalization;
using OpsBoard.Contracts.Http;

namespace OpsBoard.Api.Diagnostics;

internal static class IncidentMetricsResponse
{
    public static void Write(
        HttpResponse response,
        string strategy,
        DbCommandMetrics commandMetrics,
        TimeSpan elapsed)
    {
        var queryDuration = commandMetrics.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture);
        var dataAccessDuration = elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture);

        response.Headers[IncidentMetricsHeaders.Strategy] = strategy;
        response.Headers[IncidentMetricsHeaders.QueryCount] =
            commandMetrics.CommandCount.ToString(CultureInfo.InvariantCulture);
        response.Headers[IncidentMetricsHeaders.QueryDuration] = queryDuration;
        response.Headers[IncidentMetricsHeaders.DataAccessElapsed] = dataAccessDuration;
        response.Headers[IncidentMetricsHeaders.ServerTiming] =
            $"db-queries;dur={queryDuration}, data-access;dur={dataAccessDuration}";
    }
}
