namespace OpsBoard.Data.Model;

public sealed class IncidentTag
{
    public long IncidentId { get; set; }
    public long TagId { get; set; }

    public Incident Incident { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
