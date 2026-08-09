using Microsoft.EntityFrameworkCore;
using OpsBoard.Data;

namespace OpsBoard.IntegrationTests;

internal static class TestDbContextFactory
{
    private const string ModelOnlyConnectionString =
        "Host=localhost;Database=opsboard;Username=postgres;Password=postgres";

    public static OpsBoardDbContext Create()
    {
        var options = new DbContextOptionsBuilder<OpsBoardDbContext>()
            .UseNpgsql(ModelOnlyConnectionString)
            .Options;

        return new OpsBoardDbContext(options);
    }
}
