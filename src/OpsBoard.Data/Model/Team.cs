namespace OpsBoard.Data.Model;

public sealed class Team
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }

    public ICollection<Service> Services { get; } = [];
}
