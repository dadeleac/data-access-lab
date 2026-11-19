using System.Globalization;
using System.Security.Cryptography;

internal interface IScenarioSample
{
    string Strategy { get; }
    int ItemCount { get; }
    int QueryCount { get; }
    string BodySha256 { get; }
}

internal static class ScenarioSupport
{
    public static void ValidateSample(
        IScenarioSample sample,
        int limit,
        int sampleNumber,
        int expectedQueryCount,
        string expectedBodySha256)
    {
        var context = $"Strategy '{sample.Strategy}', limit {limit}, sample {sampleNumber}";

        if (sample.ItemCount != limit)
        {
            throw new InvalidOperationException(
                $"{context}: returned {sample.ItemCount} items; expected {limit}.");
        }

        if (sample.QueryCount != expectedQueryCount)
        {
            throw new InvalidOperationException(
                $"{context}: executed {sample.QueryCount} commands; expected {expectedQueryCount}.");
        }

        if (sample.BodySha256 != expectedBodySha256)
        {
            throw new InvalidOperationException($"{context}: returned a different response body.");
        }
    }

    public static int ReadIntHeader(HttpResponseMessage response, string name) =>
        int.Parse(ReadHeader(response, name), CultureInfo.InvariantCulture);

    public static double ReadDoubleHeader(HttpResponseMessage response, string name) =>
        double.Parse(ReadHeader(response, name), CultureInfo.InvariantCulture);

    public static string ReadHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.Single()
            : throw new InvalidOperationException(
                $"The API response did not contain the required '{name}' header.");

    public static string Hash(byte[] body) =>
        Convert.ToHexStringLower(SHA256.HashData(body));

    public static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return ordered[ordered.Length / 2];
    }
}
