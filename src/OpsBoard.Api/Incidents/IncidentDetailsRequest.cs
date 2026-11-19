using OpsBoard.Contracts.Http;

namespace OpsBoard.Api.Incidents;

public sealed record IncidentDetailsRequest(
    IncidentDetailsStrategy Strategy,
    int Limit)
{
    public static bool TryCreate(
        string? strategy,
        int? limit,
        out IncidentDetailsRequest? request,
        out IReadOnlyDictionary<string, string[]> errors)
    {
        var validationErrors = new Dictionary<string, string[]>();
        var selectedLimit = limit ?? ActiveIncidentDetailsContract.DefaultLimit;

        if (!ActiveIncidentDetailsContract.SupportedLimits.Contains(selectedLimit))
        {
            validationErrors[nameof(limit)] =
            [
                "The supported limits are: "
                + $"{string.Join(", ", ActiveIncidentDetailsContract.SupportedLimits)}."
            ];
        }

        if (!TryParseStrategy(strategy, out var selectedStrategy))
        {
            validationErrors[nameof(strategy)] =
            [
                "The supported strategies are: "
                + $"{ActiveIncidentDetailsContract.SupportedStrategiesDisplay}."
            ];
        }

        if (validationErrors.Count > 0)
        {
            request = null;
            errors = validationErrors;
            return false;
        }

        request = new IncidentDetailsRequest(selectedStrategy, selectedLimit);
        errors = validationErrors;
        return true;
    }

    private static bool TryParseStrategy(
        string? value,
        out IncidentDetailsStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                ActiveIncidentDetailsContract.Strategies.SingleQuery,
                StringComparison.OrdinalIgnoreCase))
        {
            strategy = IncidentDetailsStrategy.SingleQuery;
            return true;
        }

        if (string.Equals(
                value,
                ActiveIncidentDetailsContract.Strategies.SplitQuery,
                StringComparison.OrdinalIgnoreCase))
        {
            strategy = IncidentDetailsStrategy.SplitQuery;
            return true;
        }

        strategy = default;
        return false;
    }
}
