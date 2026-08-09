namespace OpsBoard.Data.Model;

public sealed class IncidentPause
{
    public long Id { get; set; }
    public long IncidentId { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public Incident Incident { get; set; } = null!;
}
