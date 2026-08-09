namespace OpsBoard.Data.Model;

public sealed class SlaTarget
{
    public long SlaPolicyId { get; set; }
    public IncidentSeverity Severity { get; set; }
    public int ResolutionMinutes { get; set; }

    public SlaPolicy SlaPolicy { get; set; } = null!;
}
