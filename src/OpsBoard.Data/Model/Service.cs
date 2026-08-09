namespace OpsBoard.Data.Model;

public sealed class Service
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long SlaPolicyId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;

    public Team Team { get; set; } = null!;
    public SlaPolicy SlaPolicy { get; set; } = null!;
    public ICollection<Incident> Incidents { get; } = [];
}
