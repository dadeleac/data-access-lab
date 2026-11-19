using System.Text;
using OpsBoard.Contracts.Configuration;
using OpsBoard.Data.Model;

namespace OpsBoard.DataGenerator.Generation;

public sealed class SmallDatasetFactory
{
    internal const int IncidentCount = 500;
    internal const int IncidentTagCount = 1_100;
    internal const int UpdateCount = 4_000;
    private const int PauseCount = 100;

    private static readonly string[] TeamNames =
    [
        "Platform Operations",
        "Customer Experience",
        "Payments",
        "Identity",
        "Data Platform",
        "Developer Experience",
        "Fulfilment",
        "Security Operations"
    ];

    private static readonly string[] ServiceNames =
    [
        "API Gateway",
        "Authentication",
        "Authorization",
        "Checkout",
        "Billing",
        "Payment Processor",
        "Refunds",
        "Customer Portal",
        "Notification Service",
        "Email Delivery",
        "Search",
        "Product Catalogue",
        "Inventory",
        "Order Management",
        "Shipment Tracking",
        "Data Ingestion",
        "Analytics API",
        "Reporting",
        "Feature Flags",
        "Configuration",
        "Audit Log",
        "Secrets Management",
        "Build Service",
        "Artifact Registry",
        "Deployment Service",
        "Observability",
        "Incident Webhooks",
        "File Storage",
        "Image Processing",
        "Background Jobs"
    ];

    private static readonly (string Name, string Color)[] TagDefinitions =
    [
        ("customer-impact", "#dc2626"),
        ("degraded", "#f97316"),
        ("outage", "#991b1b"),
        ("database", "#2563eb"),
        ("network", "#0891b2"),
        ("deployment", "#7c3aed"),
        ("security", "#be123c"),
        ("performance", "#ea580c"),
        ("data-loss", "#9f1239"),
        ("third-party", "#4f46e5"),
        ("configuration", "#0f766e"),
        ("capacity", "#a16207"),
        ("intermittent", "#9333ea"),
        ("regression", "#c026d3"),
        ("monitoring", "#0284c7"),
        ("manual-action", "#475569"),
        ("follow-up", "#16a34a"),
        ("sla-risk", "#e11d48"),
        ("internal", "#64748b"),
        ("automation", "#059669"),
        ("api", "#0369a1"),
        ("worker", "#4338ca"),
        ("storage", "#b45309"),
        ("authentication", "#be185d")
    ];

    public GeneratedDataset Build(DatasetOptions options)
    {
        options.Validate();
        var random = new DeterministicRandom(options.Seed);

        var teams = CreateTeams();
        var (slaPolicies, slaTargets) = CreateSlaPolicies();
        var services = CreateServices(teams, slaPolicies);
        var users = CreateUsers();
        var tags = CreateTags();

        var serviceAssignments = CreateServiceAssignments(services, random);
        var statuses = CreateQuota(
            random,
            (IncidentStatus.Open, 60),
            (IncidentStatus.Investigating, 40),
            (IncidentStatus.Resolved, 300),
            (IncidentStatus.Closed, 100));
        var severities = CreateQuota(
            random,
            (IncidentSeverity.Low, 250),
            (IncidentSeverity.Medium, 150),
            (IncidentSeverity.High, 75),
            (IncidentSeverity.Critical, 25));
        var assignees = CreateAssigneeAssignments(users, random);
        var updateCounts = CreateQuota(random, (0, 50), (5, 40), (7, 400), (100, 10));
        var tagCounts = CreateQuota(
            random,
            (0, 50),
            (1, 150),
            (2, 150),
            (3, 100),
            (4, 25),
            (10, 25));

        var incidents = new List<Incident>(IncidentCount);
        var incidentTags = new List<IncidentTag>(IncidentTagCount);
        var updates = new List<IncidentUpdate>(UpdateCount);
        var pauses = new List<IncidentPause>(PauseCount);
        var statusChanges = new List<IncidentStatusChange>(1_440);

        for (var index = 0; index < IncidentCount; index++)
        {
            var incident = CreateIncident(
                index,
                serviceAssignments[index],
                assignees[index],
                statuses[index],
                severities[index],
                options.ReferenceTime,
                random);

            incidents.Add(incident);
            incident.Service.Incidents.Add(incident);
            incident.Assignee?.AssignedIncidents.Add(incident);

            AddTags(incident, tags, tagCounts[index], random, incidentTags);
            AddUpdates(
                incident,
                users,
                updateCounts[index],
                options.ReferenceTime,
                random,
                updates);
            AddStatusChanges(incident, users, random, statusChanges);
        }

        AddPauses(incidents, options.ReferenceTime, random, pauses);
        CompleteDerivedIncidentValues(incidents, slaTargets, options.ReferenceTime);

        return new GeneratedDataset(
            teams,
            services,
            users,
            tags,
            slaPolicies,
            slaTargets,
            incidents,
            incidentTags,
            updates,
            pauses,
            statusChanges);
    }

    private static List<Team> CreateTeams() =>
        TeamNames
            .Select(name => new Team
            {
                Name = name,
                Slug = ToSlug(name)
            })
            .ToList();

    private static (List<SlaPolicy> Policies, List<SlaTarget> Targets)
        CreateSlaPolicies()
    {
        var definitions = new[]
        {
            (
                Name: "Customer Critical",
                Minutes: new Dictionary<IncidentSeverity, int>
                {
                    [IncidentSeverity.Low] = 1_440,
                    [IncidentSeverity.Medium] = 480,
                    [IncidentSeverity.High] = 120,
                    [IncidentSeverity.Critical] = 30
                }),
            (
                Name: "Business Hours",
                Minutes: new Dictionary<IncidentSeverity, int>
                {
                    [IncidentSeverity.Low] = 2_880,
                    [IncidentSeverity.Medium] = 960,
                    [IncidentSeverity.High] = 240,
                    [IncidentSeverity.Critical] = 60
                }),
            (
                Name: "Internal",
                Minutes: new Dictionary<IncidentSeverity, int>
                {
                    [IncidentSeverity.Low] = 10_080,
                    [IncidentSeverity.Medium] = 2_880,
                    [IncidentSeverity.High] = 720,
                    [IncidentSeverity.Critical] = 120
                })
        };

        var policies = new List<SlaPolicy>(definitions.Length);
        var targets = new List<SlaTarget>(definitions.Length * 4);

        foreach (var definition in definitions)
        {
            var policy = new SlaPolicy { Name = definition.Name };
            policies.Add(policy);

            foreach (var targetDefinition in definition.Minutes)
            {
                var target = new SlaTarget
                {
                    SlaPolicy = policy,
                    Severity = targetDefinition.Key,
                    ResolutionMinutes = targetDefinition.Value
                };

                policy.Targets.Add(target);
                targets.Add(target);
            }
        }

        return (policies, targets);
    }

    private static List<Service> CreateServices(
        IReadOnlyList<Team> teams,
        IReadOnlyList<SlaPolicy> policies)
    {
        var services = new List<Service>(ServiceNames.Length);

        for (var index = 0; index < ServiceNames.Length; index++)
        {
            var team = teams[index % teams.Count];
            var policy = policies[index % policies.Count];
            var service = new Service
            {
                Name = ServiceNames[index],
                Slug = ToSlug(ServiceNames[index]),
                Team = team,
                SlaPolicy = policy
            };

            team.Services.Add(service);
            policy.Services.Add(service);
            services.Add(service);
        }

        return services;
    }

    private static List<User> CreateUsers()
    {
        var users = new List<User>(150);

        for (var index = 1; index <= 150; index++)
        {
            users.Add(new User
            {
                DisplayName = $"Operator {index:000}",
                Email = $"operator{index:000}@opsboard.local",
                IsActive = index <= 145
            });
        }

        return users;
    }

    private static List<Tag> CreateTags() =>
        TagDefinitions
            .Select(definition => new Tag
            {
                Name = definition.Name,
                Slug = definition.Name,
                Color = definition.Color
            })
            .ToList();

    private static List<Service> CreateServiceAssignments(
        IReadOnlyList<Service> services,
        DeterministicRandom random)
    {
        var assignments = new List<Service>(500);

        for (var index = 0; index < 5; index++)
        {
            assignments.AddRange(Enumerable.Repeat(services[index], 60));
        }

        for (var index = 5; index < services.Count; index++)
        {
            assignments.AddRange(Enumerable.Repeat(services[index], 8));
        }

        random.Shuffle(assignments);
        return assignments;
    }

    private static List<User?> CreateAssigneeAssignments(
        IReadOnlyList<User> users,
        DeterministicRandom random)
    {
        var activeUsers = users.Where(user => user.IsActive).ToArray();
        var assignments = new List<User?>(500);
        assignments.AddRange(Enumerable.Repeat<User?>(null, 50));

        for (var index = 0; index < 450; index++)
        {
            assignments.Add(activeUsers[index % activeUsers.Length]);
        }

        random.Shuffle(assignments);
        return assignments;
    }

    private static List<T> CreateQuota<T>(
        DeterministicRandom random,
        params (T Value, int Count)[] quotas)
    {
        var values = new List<T>(quotas.Sum(quota => quota.Count));

        foreach (var quota in quotas)
        {
            values.AddRange(Enumerable.Repeat(quota.Value, quota.Count));
        }

        random.Shuffle(values);
        return values;
    }

    private static Incident CreateIncident(
        int index,
        Service service,
        User? assignee,
        IncidentStatus status,
        IncidentSeverity severity,
        DateTimeOffset referenceTime,
        DeterministicRandom random)
    {
        var age = TimeSpan.FromDays(random.Next(1, 181))
            + TimeSpan.FromMinutes(random.Next(0, 1_440));
        var createdAt = referenceTime - age;
        var targetMinutes = GetResolutionMinutes(service.SlaPolicy, severity);

        DateTimeOffset? resolvedAt = null;
        DateTimeOffset? closedAt = null;

        if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            var resolutionFactor = 0.45 + (random.NextDouble() * 1.8);
            resolvedAt = createdAt.AddMinutes(targetMinutes * resolutionFactor);
            resolvedAt = Min(resolvedAt.Value, referenceTime.AddHours(-2));
        }

        if (status is IncidentStatus.Closed)
        {
            closedAt = resolvedAt!.Value.AddHours(random.Next(1, 73));
            closedAt = Min(closedAt.Value, referenceTime.AddMinutes(-1));
        }

        var acknowledgementLimit = resolvedAt ?? closedAt ?? referenceTime;
        DateTimeOffset? acknowledgedAt =
            severity != IncidentSeverity.Low || random.Next(5) != 0
            ? Min(
                createdAt.AddMinutes(random.Next(5, 121)),
                acknowledgementLimit)
            : null;

        var title = $"{GetSeverityPrefix(severity)} {service.Name} incident {index + 1:000}";
        var descriptionLength = random.Next(240, 2_401);

        return new Incident
        {
            Number = $"INC-{index + 1:000000}",
            Service = service,
            Assignee = assignee,
            Title = title,
            Description = BuildText(
                $"Operational impact detected in {service.Name}. ",
                descriptionLength),
            Status = status,
            Severity = severity,
            CreatedAt = createdAt,
            AcknowledgedAt = acknowledgedAt,
            ResolvedAt = resolvedAt,
            ClosedAt = closedAt,
            LastActivityAt = createdAt,
            SlaDueAt = createdAt.AddMinutes(targetMinutes)
        };
    }

    private static void AddTags(
        Incident incident,
        IReadOnlyList<Tag> tags,
        int count,
        DeterministicRandom random,
        ICollection<IncidentTag> incidentTags)
    {
        var start = random.Next(tags.Count);

        for (var index = 0; index < count; index++)
        {
            var tag = tags[(start + (index * 5)) % tags.Count];
            var incidentTag = new IncidentTag
            {
                Incident = incident,
                Tag = tag
            };

            incident.IncidentTags.Add(incidentTag);
            tag.IncidentTags.Add(incidentTag);
            incidentTags.Add(incidentTag);
        }
    }

    private static void AddUpdates(
        Incident incident,
        IReadOnlyList<User> users,
        int count,
        DateTimeOffset referenceTime,
        DeterministicRandom random,
        ICollection<IncidentUpdate> updates)
    {
        if (count == 0)
        {
            return;
        }

        var end = incident.ClosedAt
            ?? incident.ResolvedAt
            ?? referenceTime.AddMinutes(-1);
        var availableTicks = Math.Max(1, (end - incident.CreatedAt).Ticks);

        for (var index = 0; index < count; index++)
        {
            var fraction = (index + 1d) / (count + 1d);
            var createdAt = incident.CreatedAt.AddTicks((long)(availableTicks * fraction));
            var kind = GetUpdateKind(incident.Status, index, count);
            var author = users[(random.Next(users.Count - 5) + index) % (users.Count - 5)];
            var bodyLength = random.Next(80, index % 17 == 0 ? 1_201 : 401);
            var update = new IncidentUpdate
            {
                Incident = incident,
                Author = author,
                Kind = kind,
                Body = BuildText($"{kind} update for {incident.Number}. ", bodyLength),
                CreatedAt = createdAt
            };

            incident.Updates.Add(update);
            author.Updates.Add(update);
            updates.Add(update);
        }
    }

    private static IncidentUpdateKind GetUpdateKind(
        IncidentStatus status,
        int index,
        int count)
    {
        if (index == count - 1
            && status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            return IncidentUpdateKind.Resolution;
        }

        return (index % 4) switch
        {
            0 => IncidentUpdateKind.Comment,
            1 => IncidentUpdateKind.Investigation,
            2 => IncidentUpdateKind.Mitigation,
            _ => IncidentUpdateKind.Comment
        };
    }

    private static void AddStatusChanges(
        Incident incident,
        IReadOnlyList<User> users,
        DeterministicRandom random,
        ICollection<IncidentStatusChange> statusChanges)
    {
        AddStatusChange(
            incident,
            users,
            random,
            null,
            IncidentStatus.Open,
            incident.CreatedAt,
            statusChanges);

        if (incident.Status == IncidentStatus.Open)
        {
            return;
        }

        var investigationAt = incident.CreatedAt.AddMinutes(
            Math.Max(
                1,
                (LastTerminalTime(incident) - incident.CreatedAt).TotalMinutes * 0.2));
        AddStatusChange(
            incident,
            users,
            random,
            IncidentStatus.Open,
            IncidentStatus.Investigating,
            investigationAt,
            statusChanges);

        if (incident.Status == IncidentStatus.Investigating)
        {
            return;
        }

        AddStatusChange(
            incident,
            users,
            random,
            IncidentStatus.Investigating,
            IncidentStatus.Resolved,
            incident.ResolvedAt!.Value,
            statusChanges);

        if (incident.Status == IncidentStatus.Closed)
        {
            AddStatusChange(
                incident,
                users,
                random,
                IncidentStatus.Resolved,
                IncidentStatus.Closed,
                incident.ClosedAt!.Value,
                statusChanges);
        }
    }

    private static void AddStatusChange(
        Incident incident,
        IReadOnlyList<User> users,
        DeterministicRandom random,
        IncidentStatus? fromStatus,
        IncidentStatus toStatus,
        DateTimeOffset changedAt,
        ICollection<IncidentStatusChange> statusChanges)
    {
        var changedBy = users[random.Next(users.Count - 5)];
        var statusChange = new IncidentStatusChange
        {
            Incident = incident,
            ChangedBy = changedBy,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedAt = changedAt
        };

        incident.StatusChanges.Add(statusChange);
        changedBy.StatusChanges.Add(statusChange);
        statusChanges.Add(statusChange);
    }

    private static void AddPauses(
        IReadOnlyList<Incident> incidents,
        DateTimeOffset referenceTime,
        DeterministicRandom random,
        ICollection<IncidentPause> pauses)
    {
        var candidates = incidents.ToList();
        random.Shuffle(candidates);

        foreach (var incident in candidates)
        {
            var terminalTime = LastTerminalTime(incident, referenceTime);
            var availableTime = terminalTime - incident.CreatedAt;

            if (availableTime <= TimeSpan.FromMinutes(30))
            {
                continue;
            }

            var availableMinutes = Math.Max(
                60,
                (int)availableTime.TotalMinutes);
            var startOffset = random.Next(30, Math.Max(31, availableMinutes / 2));
            var startedAt = incident.CreatedAt.AddMinutes(startOffset);
            var index = pauses.Count;
            var isActive = incident.Status is IncidentStatus.Open or IncidentStatus.Investigating
                && index % 10 == 0;
            DateTimeOffset? endedAt = isActive
                ? null
                : Min(
                    startedAt.AddMinutes(random.Next(30, 241)),
                    terminalTime);

            var pause = new IncidentPause
            {
                Incident = incident,
                Reason = (index % 3) switch
                {
                    0 => "Waiting for an external provider",
                    1 => "Waiting for customer confirmation",
                    _ => "Scheduled operational hold"
                },
                StartedAt = startedAt,
                EndedAt = endedAt
            };

            incident.Pauses.Add(pause);
            pauses.Add(pause);

            if (pauses.Count == PauseCount)
            {
                break;
            }
        }
    }

    private static void CompleteDerivedIncidentValues(
        IEnumerable<Incident> incidents,
        IEnumerable<SlaTarget> targets,
        DateTimeOffset referenceTime)
    {
        var targetLookup = targets.ToDictionary(
            target => (target.SlaPolicy, target.Severity),
            target => target.ResolutionMinutes);

        foreach (var incident in incidents)
        {
            var pauseMinutes = incident.Pauses.Sum(
                pause => ((pause.EndedAt ?? referenceTime) - pause.StartedAt).TotalMinutes);
            var targetMinutes = targetLookup[(incident.Service.SlaPolicy, incident.Severity)];
            incident.SlaDueAt = incident.CreatedAt.AddMinutes(targetMinutes + pauseMinutes);

            var activityTimes = incident.Updates.Select(update => update.CreatedAt)
                .Concat(incident.StatusChanges.Select(change => change.ChangedAt))
                .Concat(incident.Pauses.Select(pause => pause.EndedAt ?? pause.StartedAt))
                .Append(incident.CreatedAt);

            incident.LastActivityAt = activityTimes.Max();
        }
    }

    private static int GetResolutionMinutes(
        SlaPolicy policy,
        IncidentSeverity severity) =>
        policy.Targets.Single(target => target.Severity == severity).ResolutionMinutes;

    private static string GetSeverityPrefix(IncidentSeverity severity) =>
        severity switch
        {
            IncidentSeverity.Low => "Minor",
            IncidentSeverity.Medium => "Degraded",
            IncidentSeverity.High => "Major",
            IncidentSeverity.Critical => "Critical",
            _ => throw new ArgumentOutOfRangeException(nameof(severity))
        };

    private static DateTimeOffset LastTerminalTime(
        Incident incident,
        DateTimeOffset? fallback = null) =>
        incident.ClosedAt
        ?? incident.ResolvedAt
        ?? fallback
        ?? incident.CreatedAt.AddHours(12);

    private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) =>
        first <= second ? first : second;

    private static string BuildText(string sentence, int minimumLength)
    {
        var builder = new StringBuilder(minimumLength + sentence.Length);

        while (builder.Length < minimumLength)
        {
            builder.Append(sentence);
        }

        return builder.ToString(0, minimumLength);
    }

    private static string ToSlug(string value) =>
        value.ToLowerInvariant().Replace(' ', '-');
}
