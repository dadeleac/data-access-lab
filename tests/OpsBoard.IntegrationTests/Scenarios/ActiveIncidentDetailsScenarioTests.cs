using System.Net;
using System.Text.Json;
using OpsBoard.Contracts.Http;

namespace OpsBoard.IntegrationTests.Scenarios;

public sealed class ActiveIncidentDetailsScenarioTests
{
    [Theory]
    [InlineData(2, 9, 1, "original", "sample 1: returned 9 items; expected 10")]
    [InlineData(3, 10, 1, "changed", "sample 2: returned a different response body")]
    [InlineData(4, 10, 2, "original", "sample 3: executed 2 commands; expected 1")]
    [InlineData(6, 10, 3, "changed", "sample 1: returned a different response body")]
    public async Task Rejects_incomplete_or_inconsistent_measurements(
        int changedRequest,
        int itemCount,
        int queryCount,
        string title,
        string expectedError)
    {
        using var client = new HttpClient(new ScenarioHandler(
            changedRequest, itemCount, queryCount, title))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var scenario = new ActiveIncidentDetailsScenario(
            client, TimeProvider.System, verifyReferenceCardinality: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.RunAsync());

        Assert.Contains("limit 10", error.Message, StringComparison.Ordinal);
        Assert.Contains(expectedError, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_reference_results_with_missing_collections()
    {
        using var client = new HttpClient(new ScenarioHandler(0, 10, 1, "original"))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var scenario = new ActiveIncidentDetailsScenario(
            client, TimeProvider.System, verifyReferenceCardinality: true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.RunAsync());

        Assert.Contains("calculated 10 structural rows", error.Message, StringComparison.Ordinal);
        Assert.Contains("expected 454", error.Message, StringComparison.Ordinal);
    }

    private sealed class ScenarioHandler(
        int changedRequest,
        int itemCount,
        int queryCount,
        string title) : HttpMessageHandler
    {
        private int requestCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            requestCount++;
            var isChanged = requestCount == changedRequest;
            var isSplit = request.RequestUri!.Query.Contains(
                ActiveIncidentDetailsContract.Strategies.SplitQuery, StringComparison.Ordinal);
            var items = Enumerable.Repeat(new
            {
                title = isChanged ? title : "original",
                tags = Array.Empty<string>(),
                updates = Array.Empty<string>()
            }, isChanged ? itemCount : 10);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(items))
            };
            var commands = isChanged ? queryCount : (isSplit ? 3 : 1);
            response.Headers.Add(IncidentMetricsHeaders.QueryCount, commands.ToString());
            response.Headers.Add(IncidentMetricsHeaders.QueryDuration, "1");
            response.Headers.Add(IncidentMetricsHeaders.DataAccessElapsed, "2");
            return Task.FromResult(response);
        }
    }
}
