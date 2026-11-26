namespace OpsBoard.Api.Diagnostics;

internal sealed class ManagedMemorySampler : IAsyncDisposable
{
    private readonly PeriodicTimer timer = new(TimeSpan.FromMilliseconds(10));
    private readonly long baselineManagedBytes = GC.GetTotalMemory(forceFullCollection: false);
    private readonly long startingAllocations = GC.GetTotalAllocatedBytes(precise: true);
    private readonly Task sampling;
    private readonly Lock sampleLock = new();
    private long peakManagedBytes;

    public ManagedMemorySampler()
    {
        peakManagedBytes = baselineManagedBytes;
        sampling = SamplePeriodicallyAsync();
    }

    public void Sample()
    {
        var managedBytes = GC.GetTotalMemory(forceFullCollection: false);
        lock (sampleLock)
        {
            peakManagedBytes = Math.Max(peakManagedBytes, managedBytes);
        }
    }

    public (long AllocatedBytes, long BaselineManagedBytes, long PeakManagedBytes) Snapshot()
    {
        Sample();
        lock (sampleLock)
        {
            return (GC.GetTotalAllocatedBytes(precise: true) - startingAllocations,
                baselineManagedBytes, peakManagedBytes);
        }
    }

    public async ValueTask DisposeAsync()
    {
        timer.Dispose();
        await sampling;
    }

    private async Task SamplePeriodicallyAsync()
    {
        while (await timer.WaitForNextTickAsync())
        {
            Sample();
        }
    }
}
