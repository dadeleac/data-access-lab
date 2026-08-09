namespace OpsBoard.DataGenerator.Generation;

internal sealed class DeterministicRandom(int seed)
{
    private ulong _state = unchecked((ulong)(long)seed) + 0x9E3779B97F4A7C15UL;

    public int Next(int exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);
        return (int)(NextUInt64() % (uint)exclusiveMaximum);
    }

    public int Next(int inclusiveMinimum, int exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            exclusiveMaximum,
            inclusiveMinimum);

        return inclusiveMinimum + Next(exclusiveMaximum - inclusiveMinimum);
    }

    public double NextDouble() =>
        (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public void Shuffle<T>(IList<T> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
