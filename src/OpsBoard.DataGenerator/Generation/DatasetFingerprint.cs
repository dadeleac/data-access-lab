using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OpsBoard.DataGenerator.Generation;

public static class DatasetFingerprint
{
    public static string Compute(GeneratedDataset dataset)
    {
        var content = new StringBuilder(4_000_000);

        foreach (var team in dataset.Teams)
        {
            Append(content, "team", team.Slug, team.Name);
        }

        foreach (var service in dataset.Services)
        {
            Append(
                content,
                "service",
                service.Slug,
                service.Name,
                service.Team.Slug,
                service.SlaPolicy.Name,
                service.IsActive);
        }

        foreach (var user in dataset.Users)
        {
            Append(content, "user", user.Email, user.DisplayName, user.IsActive);
        }

        foreach (var tag in dataset.Tags)
        {
            Append(content, "tag", tag.Slug, tag.Name, tag.Color);
        }

        foreach (var target in dataset.SlaTargets)
        {
            Append(
                content,
                "sla",
                target.SlaPolicy.Name,
                target.Severity,
                target.ResolutionMinutes);
        }

        foreach (var incident in dataset.Incidents)
        {
            Append(
                content,
                "incident",
                incident.Number,
                incident.Service.Slug,
                incident.Assignee?.Email,
                incident.Title,
                incident.Description,
                incident.Status,
                incident.Severity,
                Format(incident.CreatedAt),
                Format(incident.AcknowledgedAt),
                Format(incident.ResolvedAt),
                Format(incident.ClosedAt),
                Format(incident.LastActivityAt),
                Format(incident.SlaDueAt));

            foreach (var incidentTag in incident.IncidentTags)
            {
                Append(content, "incident-tag", incident.Number, incidentTag.Tag.Slug);
            }

            foreach (var update in incident.Updates)
            {
                Append(
                    content,
                    "update",
                    incident.Number,
                    update.Author.Email,
                    update.Kind,
                    update.Body,
                    Format(update.CreatedAt));
            }

            foreach (var pause in incident.Pauses)
            {
                Append(
                    content,
                    "pause",
                    incident.Number,
                    pause.Reason,
                    Format(pause.StartedAt),
                    Format(pause.EndedAt));
            }

            foreach (var change in incident.StatusChanges)
            {
                Append(
                    content,
                    "status",
                    incident.Number,
                    change.ChangedBy.Email,
                    change.FromStatus,
                    change.ToStatus,
                    Format(change.ChangedAt));
            }
        }

        var bytes = Encoding.UTF8.GetBytes(content.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void Append(
        StringBuilder builder,
        params object?[] values)
    {
        foreach (var value in values)
        {
            builder.Append(value);
            builder.Append('\u001f');
        }

        builder.Append('\n');
    }

    private static string? Format(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);
}
