using OpsBoard.Data.Model;

namespace OpsBoard.DataGenerator.Generation;

public sealed record GeneratedDataset(
    IReadOnlyList<Team> Teams,
    IReadOnlyList<Service> Services,
    IReadOnlyList<User> Users,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<SlaPolicy> SlaPolicies,
    IReadOnlyList<SlaTarget> SlaTargets,
    IReadOnlyList<Incident> Incidents,
    IReadOnlyList<IncidentTag> IncidentTags,
    IReadOnlyList<IncidentUpdate> Updates,
    IReadOnlyList<IncidentPause> Pauses,
    IReadOnlyList<IncidentStatusChange> StatusChanges);
