namespace OpsBoard.Api.Diagnostics;

public sealed class DbCommandMetrics
{
    private int _commandCount;
    private long _elapsedTicks;

    public int CommandCount => Volatile.Read(ref _commandCount);

    public TimeSpan Elapsed => TimeSpan.FromTicks(Interlocked.Read(ref _elapsedTicks));

    public void Reset()
    {
        Interlocked.Exchange(ref _commandCount, 0);
        Interlocked.Exchange(ref _elapsedTicks, 0);
    }

    public void Record(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _commandCount);
        Interlocked.Add(ref _elapsedTicks, elapsed.Ticks);
    }
}
