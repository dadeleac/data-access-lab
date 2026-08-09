using Microsoft.EntityFrameworkCore;
using OpsBoard.Data.Model;

namespace OpsBoard.Data;

public sealed class OpsBoardDbContext(DbContextOptions<OpsBoardDbContext> options)
    : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<SlaTarget> SlaTargets => Set<SlaTarget>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<IncidentTag> IncidentTags => Set<IncidentTag>();
    public DbSet<IncidentUpdate> IncidentUpdates => Set<IncidentUpdate>();
    public DbSet<IncidentPause> IncidentPauses => Set<IncidentPause>();
    public DbSet<IncidentStatusChange> IncidentStatusChanges => Set<IncidentStatusChange>();
    public DbSet<DatasetMetadata> DatasetMetadata => Set<DatasetMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpsBoardDbContext).Assembly);
    }
}
