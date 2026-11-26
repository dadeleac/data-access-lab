namespace OpsBoard.Contracts.Configuration;

public sealed class ScenarioOptions
{
    public const string ConfigurationSectionName = "Scenarios";

    public bool Run { get; set; }
    public string? Lab04Strategy { get; set; }
}
