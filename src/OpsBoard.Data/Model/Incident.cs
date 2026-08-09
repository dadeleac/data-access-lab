namespace OpsBoard.Data.Model;

public sealed class Incident
{
    public long Id { get; set; }
    public required string Number { get; set; }
    public long ServiceId { get; set; }
    public long? AssigneeId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public IncidentStatus Status { get; set; }
    public IncidentSeverity Severity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public DateTimeOffset SlaDueAt { get; set; }

    public Service Service { get; set; } = null!;
    public User? Assignee { get; set; }
    public ICollection<IncidentTag> IncidentTags { get; } = [];
    public ICollection<IncidentUpdate> Updates { get; } = [];
    public ICollection<IncidentPause> Pauses { get; } = [];
    public ICollection<IncidentStatusChange> StatusChanges { get; } = [];
}
