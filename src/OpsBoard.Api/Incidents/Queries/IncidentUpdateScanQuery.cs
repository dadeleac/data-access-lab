using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpsBoard.Api.Diagnostics;
using OpsBoard.Contracts.Http;
using OpsBoard.Data;
using OpsBoard.Data.Model;

namespace OpsBoard.Api.Incidents.Queries;

internal static class IncidentUpdateScanQuery
{
    public static IQueryable<IncidentUpdate> Create(OpsBoardDbContext dbContext, string strategy, int limit)
    {
        var query = dbContext.IncidentUpdates
            .TagWith("Lab 04: scan updates and their authors")
            .Include(update => update.Author)
            .OrderBy(update => update.Id)
            .Take(limit);

        return strategy switch
        {
            IncidentUpdateScanContract.Strategies.Tracking => query.AsTracking(),
            IncidentUpdateScanContract.Strategies.IdentityResolution => query.AsNoTrackingWithIdentityResolution(),
            IncidentUpdateScanContract.Strategies.NoTracking or IncidentUpdateScanContract.Strategies.Buffered =>
                query.AsNoTracking(),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown scan strategy.")
        };
    }

    public static async Task<IncidentUpdateScanResult> ExecuteAsync(
        OpsBoardDbContext dbContext,
        string strategy,
        int limit,
        ManagedMemorySampler? memory,
        CancellationToken cancellationToken)
    {
        var query = Create(dbContext, strategy, limit);
        using var checksum = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var authorIds = new HashSet<long>();
        var itemCount = 0;

        if (strategy == IncidentUpdateScanContract.Strategies.Buffered)
        {
            var updates = await query.ToListAsync(cancellationToken);
            memory?.Sample();
            foreach (var update in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Process(update);
            }
        }
        else
        {
            await foreach (var update in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                Process(update);
            }
        }

        return new IncidentUpdateScanResult(itemCount, authorIds.Count,
            Convert.ToHexStringLower(checksum.GetHashAndReset()));

        void Process(IncidentUpdate update)
        {
            var authorId = update.Author.Id;
            authorIds.Add(authorId);
            itemCount++;

            Span<byte> fields = stackalloc byte[28];
            BinaryPrimitives.WriteInt64LittleEndian(fields, update.Id);
            BinaryPrimitives.WriteInt64LittleEndian(fields[8..], authorId);
            BinaryPrimitives.WriteInt32LittleEndian(fields[16..], (int)update.Kind);
            BinaryPrimitives.WriteInt64LittleEndian(fields[20..], update.CreatedAt.UtcTicks);
            checksum.AppendData(fields);
        }
    }
}
