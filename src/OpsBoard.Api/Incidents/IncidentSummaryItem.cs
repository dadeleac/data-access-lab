using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents;

public sealed record IncidentSummaryItem(
    string Number,
    string Title,
    IncidentStatus Status,
    IncidentSeverity Severity,
    string Service,
    string Team,
    string? Assignee,
    DateTimeOffset LastActivityAt);
