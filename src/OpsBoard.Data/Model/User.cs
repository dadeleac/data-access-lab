namespace OpsBoard.Data.Model;

public sealed class User
{
    public long Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Incident> AssignedIncidents { get; } = [];
    public ICollection<IncidentUpdate> Updates { get; } = [];
    public ICollection<IncidentStatusChange> StatusChanges { get; } = [];
}
