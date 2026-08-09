namespace OpsBoard.Data.Model;

public sealed class IncidentUpdate
{
    public long Id { get; set; }
    public long IncidentId { get; set; }
    public long AuthorId { get; set; }
    public IncidentUpdateKind Kind { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Incident Incident { get; set; } = null!;
    public User Author { get; set; } = null!;
}
