using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents;

public sealed record IncidentListItem(
    string Number,
    string Title,
    IncidentSeverity Severity,
    string Service,
    string Team,
    string? Assignee,
    IReadOnlyList<string> Tags,
    int UpdateCount,
    DateTimeOffset LastActivityAt);
