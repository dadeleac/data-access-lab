using OpsBoard.Api.Diagnostics;

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
}
