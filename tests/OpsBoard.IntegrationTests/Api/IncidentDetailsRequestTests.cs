using OpsBoard.Api.Incidents;
using OpsBoard.Contracts.Http;

namespace OpsBoard.IntegrationTests.Api;

public sealed class IncidentDetailsRequestTests
{
    [Fact]
    public void Missing_values_select_single_query_and_default_limit()
    {
        var isValid = IncidentDetailsRequest.TryCreate(
            strategy: null,
            limit: null,
            out var request,
            out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(IncidentDetailsStrategy.SingleQuery, request?.Strategy);
        Assert.Equal(50, request?.Limit);
    }

    [Theory]
    [InlineData(
        ActiveIncidentDetailsContract.Strategies.SingleQuery,
        IncidentDetailsStrategy.SingleQuery)]
    [InlineData("SPLIT-QUERY", IncidentDetailsStrategy.SplitQuery)]
    public void Supported_strategies_are_case_insensitive(
        string value,
        IncidentDetailsStrategy expected)
    {
        var isValid = IncidentDetailsRequest.TryCreate(
            value,
            25,
            out var request,
            out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(expected, request?.Strategy);
    }

    [Fact]
    public void Invalid_strategy_and_limit_report_both_contract_errors()
    {
        var isValid = IncidentDetailsRequest.TryCreate(
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
