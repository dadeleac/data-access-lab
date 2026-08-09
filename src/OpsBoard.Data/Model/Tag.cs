namespace OpsBoard.Data.Model;

public sealed class Tag
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Color { get; set; }

    public ICollection<IncidentTag> IncidentTags { get; } = [];
}
