namespace OpsBoard.Data.Model;

public sealed class DatasetMetadata
{
    public int Id { get; set; }
    public required string Profile { get; set; }
    public int Seed { get; set; }
    public DateTimeOffset ReferenceTime { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public required string ModelVersion { get; set; }
}
