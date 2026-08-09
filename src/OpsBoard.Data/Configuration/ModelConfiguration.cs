using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsBoard.Data.Model;

namespace OpsBoard.Data.Configuration;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.Slug).HasMaxLength(120);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(160);
        builder.Property(x => x.Slug).HasMaxLength(160);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne(x => x.Team)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SlaPolicy)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.SlaPolicyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.DisplayName).HasMaxLength(160);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.HasIndex(x => x.Email).IsUnique();
    }
}

internal sealed class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class SlaTargetConfiguration : IEntityTypeConfiguration<SlaTarget>
{
    public void Configure(EntityTypeBuilder<SlaTarget> builder)
    {
        builder.HasKey(x => new { x.SlaPolicyId, x.Severity });

        builder.HasOne(x => x.SlaPolicy)
            .WithMany(x => x.Targets)
            .HasForeignKey(x => x.SlaPolicyId);
    }
}

internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.Property(x => x.Number).HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(240);
        builder.Property(x => x.Description).HasColumnType("text");
        builder.HasIndex(x => x.Number).IsUnique();

        builder.HasOne(x => x.Service)
            .WithMany(x => x.Incidents)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Assignee)
            .WithMany(x => x.AssignedIncidents)
            .HasForeignKey(x => x.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(80);
        builder.Property(x => x.Slug).HasMaxLength(80);
        builder.Property(x => x.Color).HasMaxLength(16);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class IncidentTagConfiguration : IEntityTypeConfiguration<IncidentTag>
{
    public void Configure(EntityTypeBuilder<IncidentTag> builder)
    {
        builder.HasKey(x => new { x.IncidentId, x.TagId });

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.IncidentTags)
            .HasForeignKey(x => x.IncidentId);

        builder.HasOne(x => x.Tag)
            .WithMany(x => x.IncidentTags)
            .HasForeignKey(x => x.TagId);
    }
}

internal sealed class IncidentUpdateConfiguration : IEntityTypeConfiguration<IncidentUpdate>
{
    public void Configure(EntityTypeBuilder<IncidentUpdate> builder)
    {
        builder.Property(x => x.Body).HasColumnType("text");

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.Updates)
            .HasForeignKey(x => x.IncidentId);

        builder.HasOne(x => x.Author)
            .WithMany(x => x.Updates)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IncidentPauseConfiguration : IEntityTypeConfiguration<IncidentPause>
{
    public void Configure(EntityTypeBuilder<IncidentPause> builder)
    {
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.Pauses)
            .HasForeignKey(x => x.IncidentId);
    }
}

internal sealed class IncidentStatusChangeConfiguration : IEntityTypeConfiguration<IncidentStatusChange>
{
    public void Configure(EntityTypeBuilder<IncidentStatusChange> builder)
    {
        builder.HasOne(x => x.Incident)
            .WithMany(x => x.StatusChanges)
            .HasForeignKey(x => x.IncidentId);

        builder.HasOne(x => x.ChangedBy)
            .WithMany(x => x.StatusChanges)
            .HasForeignKey(x => x.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DatasetMetadataConfiguration : IEntityTypeConfiguration<DatasetMetadata>
{
    public void Configure(EntityTypeBuilder<DatasetMetadata> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Profile).HasMaxLength(32);
        builder.Property(x => x.ModelVersion).HasMaxLength(32);
    }
}
