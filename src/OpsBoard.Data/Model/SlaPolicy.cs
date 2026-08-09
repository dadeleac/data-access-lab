namespace OpsBoard.Data.Model;

public sealed class SlaPolicy
{
    public long Id { get; set; }
    public required string Name { get; set; }

    public ICollection<SlaTarget> Targets { get; } = [];
    public ICollection<Service> Services { get; } = [];
}
