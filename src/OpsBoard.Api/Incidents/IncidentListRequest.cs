using OpsBoard.Contracts.Http;

namespace OpsBoard.Api.Incidents;

public sealed record IncidentListRequest(
    IncidentListStrategy Strategy,
    int Limit)
{
    public static bool TryCreate(
        string? strategy,
        int? limit,
        out IncidentListRequest? request,
        out IReadOnlyDictionary<string, string[]> errors)
    {
        var validationErrors = new Dictionary<string, string[]>();
        var selectedLimit = limit ?? ActiveIncidentListContract.DefaultLimit;

        if (!ActiveIncidentListContract.SupportedLimits.Contains(selectedLimit))
        {
            validationErrors[nameof(limit)] =
            [
                "The supported limits are: "
                + $"{string.Join(", ", ActiveIncidentListContract.SupportedLimits)}."
            ];
        }

        if (!TryParseStrategy(strategy, out var selectedStrategy))
        {
            validationErrors[nameof(strategy)] =
            [
                $"The supported strategies are: "
                + $"{ActiveIncidentListContract.SupportedStrategiesDisplay}."
            ];
        }

        if (validationErrors.Count > 0)
        {
            request = null;
            errors = validationErrors;
            return false;
        }

        request = new IncidentListRequest(selectedStrategy, selectedLimit);
        errors = validationErrors;
        return true;
    }

    private static bool TryParseStrategy(
        string? value,
        out IncidentListStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            strategy = IncidentListStrategy.PerIncident;
            return true;
        }

        if (string.Equals(
                value,
                ActiveIncidentListContract.Strategies.PerIncident,
                StringComparison.OrdinalIgnoreCase))
        {
            strategy = IncidentListStrategy.PerIncident;
            return true;
        }

        if (string.Equals(
                value,
                ActiveIncidentListContract.Strategies.Batched,
                StringComparison.OrdinalIgnoreCase))
        {
            strategy = IncidentListStrategy.Batched;
            return true;
        }

        if (string.Equals(
                value,
                ActiveIncidentListContract.Strategies.Projected,
                StringComparison.OrdinalIgnoreCase))
        {
            strategy = IncidentListStrategy.Projected;
            return true;
        }

        strategy = default;
        return false;
    }
}
