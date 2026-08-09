using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OpsBoard.Contracts.Hosting;

namespace OpsBoard.Data;

public sealed class OpsBoardDbContextFactory : IDesignTimeDbContextFactory<OpsBoardDbContext>
{
    public OpsBoardDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                OpsBoardResourceNames.DatabaseConnectionStringEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Set {OpsBoardResourceNames.DatabaseConnectionStringEnvironmentVariable} "
                + "before running Entity Framework design-time commands.");

        var options = new DbContextOptionsBuilder<OpsBoardDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OpsBoardDbContext(options);
    }
}
