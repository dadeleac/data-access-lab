internal sealed record ScenarioRunReport(
    DateTimeOffset CapturedAt,
    ActiveIncidentListReport Lab01,
    ActiveIncidentDetailsReport Lab02);

internal sealed record ActiveIncidentListReport(
    string Scenario,
    DateTimeOffset CapturedAt,
    IReadOnlyList<ActiveIncidentListResult> Results);

internal sealed record ActiveIncidentListResult(
    string Strategy,
    int Limit,
    int ItemCount,
    int QueryCount,
    double MedianQueryExecutionMilliseconds,
    double MedianDataAccessMilliseconds,
    string BodySha256);

internal sealed record ActiveIncidentDetailsReport(
    string Scenario,
    DateTimeOffset CapturedAt,
    IReadOnlyList<ActiveIncidentDetailsResult> Results);

internal sealed record ActiveIncidentDetailsResult(
    string Strategy,
    int Limit,
    int ItemCount,
    int QueryCount,
    long ExpectedStructuralRows,
    double MedianQueryExecutionMilliseconds,
    double MedianDataAccessMilliseconds,
    string BodySha256);
