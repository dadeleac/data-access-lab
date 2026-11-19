using Microsoft.AspNetCore.Http;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Contracts.Http;

namespace OpsBoard.IntegrationTests.Api;

public sealed class DbCommandMetricsTests
{
    [Fact]
    public void Records_and_resets_commands_and_elapsed_time()
    {
        var metrics = new DbCommandMetrics();

        metrics.Record(TimeSpan.FromMilliseconds(1.25));
        metrics.Record(TimeSpan.FromMilliseconds(2.75));

        Assert.Equal(2, metrics.CommandCount);
        Assert.Equal(TimeSpan.FromMilliseconds(4), metrics.Elapsed);

        metrics.Reset();

        Assert.Equal(0, metrics.CommandCount);
        Assert.Equal(TimeSpan.Zero, metrics.Elapsed);
    }

    [Fact]
    public void Response_headers_use_the_same_durations_as_server_timing()
    {
        var metrics = new DbCommandMetrics();
        metrics.Record(TimeSpan.FromMilliseconds(1.25));
        var response = new DefaultHttpContext().Response;

        IncidentMetricsResponse.Write(response, "split-query", metrics, TimeSpan.FromMilliseconds(2.5));

        Assert.Equal("split-query", response.Headers[IncidentMetricsHeaders.Strategy].ToString());
        Assert.Equal("1", response.Headers[IncidentMetricsHeaders.QueryCount].ToString());
        Assert.Equal("1.25", response.Headers[IncidentMetricsHeaders.QueryDuration].ToString());
        Assert.Equal("2.50", response.Headers[IncidentMetricsHeaders.DataAccessElapsed].ToString());
        Assert.Equal("db-queries;dur=1.25, data-access;dur=2.50",
            response.Headers[IncidentMetricsHeaders.ServerTiming].ToString());
    }
}
