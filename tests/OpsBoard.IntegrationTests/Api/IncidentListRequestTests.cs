using OpsBoard.Api.Incidents;
using OpsBoard.Contracts.Http;

namespace OpsBoard.IntegrationTests.Api;

public sealed class IncidentListRequestTests
{
    [Fact]
    public void Missing_values_select_the_per_incident_strategy_and_fifty_items()
    {
        var isValid = IncidentListRequest.TryCreate(
            strategy: null,
            limit: null,
            out var request,
            out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(IncidentListStrategy.PerIncident, request?.Strategy);
        Assert.Equal(50, request?.Limit);
    }

    [Theory]
    [InlineData(
        ActiveIncidentListContract.Strategies.PerIncident,
        IncidentListStrategy.PerIncident)]
    [InlineData("PER-INCIDENT", IncidentListStrategy.PerIncident)]
    [InlineData(
        ActiveIncidentListContract.Strategies.Batched,
        IncidentListStrategy.Batched)]
    [InlineData(
        ActiveIncidentListContract.Strategies.Projected,
        IncidentListStrategy.Projected)]
    public void Supported_strategies_are_case_insensitive(
        string value,
        IncidentListStrategy expected)
    {
        var isValid = IncidentListRequest.TryCreate(
            value,
            25,
            out var request,
            out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(expected, request?.Strategy);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void Supported_limits_are_accepted(int limit)
    {
        var isValid = IncidentListRequest.TryCreate(
            ActiveIncidentListContract.Strategies.Projected,
            limit,
            out var request,
            out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(limit, request?.Limit);
    }

    [Fact]
    public void Invalid_values_report_both_contract_errors()
    {
        var isValid = IncidentListRequest.TryCreate(
            "unknown",
            42,
            out var request,
            out var errors);

        Assert.False(isValid);
        Assert.Null(request);
        Assert.Contains("strategy", errors.Keys);
        Assert.Contains("limit", errors.Keys);
    }
}
