namespace OpsBoard.Data.Model;

public sealed class IncidentStatusChange
{
    public long Id { get; set; }
    public long IncidentId { get; set; }
    public long ChangedById { get; set; }
    public IncidentStatus? FromStatus { get; set; }
    public IncidentStatus ToStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; }

    public Incident Incident { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}
