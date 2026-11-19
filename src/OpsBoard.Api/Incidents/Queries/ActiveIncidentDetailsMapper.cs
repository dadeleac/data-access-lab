using OpsBoard.Api.Incidents;
using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents.Queries;

internal static class ActiveIncidentDetailsMapper
{
    public static IReadOnlyList<IncidentDetailsItem> Map(
        IReadOnlyList<Incident> incidents) =>
        incidents.Select(Map).ToArray();

    private static IncidentDetailsItem Map(Incident incident) =>
        new(
            incident.Number,
            incident.Title,
            incident.Description,
            incident.Severity,
            incident.Service.Name,
            incident.Service.Team.Name,
            incident.Assignee?.DisplayName,
            incident.IncidentTags
                .OrderBy(incidentTag => incidentTag.Tag.Name)
                .ThenBy(incidentTag => incidentTag.Tag.Id)
                .Select(incidentTag => incidentTag.Tag.Name)
                .ToArray(),
            incident.Updates
                .OrderBy(update => update.CreatedAt)
                .ThenBy(update => update.Id)
                .Select(update => new IncidentUpdateItem(
                    update.Kind,
                    update.Body,
                    update.Author.DisplayName,
                    update.CreatedAt))
                .ToArray());
}
