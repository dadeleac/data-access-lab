using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents;

public sealed record IncidentDetailsItem(
    string Number,
    string Title,
    string Description,
    IncidentSeverity Severity,
    string Service,
    string Team,
    string? Assignee,
    IReadOnlyList<string> Tags,
    IReadOnlyList<IncidentUpdateItem> Updates);

public sealed record IncidentUpdateItem(
    IncidentUpdateKind Kind,
    string Body,
    string Author,
    DateTimeOffset CreatedAt);
