using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OpsBoard.Api.Diagnostics;

internal sealed class QueryShapeMetricsInterceptor : DbCommandInterceptor, IMaterializationInterceptor
{
    private readonly ConditionalWeakTable<DbContext, QueryShapeMetrics> activeMeasurements = new();

    public QueryShapeMetrics Start(DbContext dbContext)
    {
        var metrics = new QueryShapeMetrics();
        activeMeasurements.Add(dbContext, metrics);
        return metrics;
    }

    public void Stop(DbContext dbContext) => activeMeasurements.Remove(dbContext);

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } dbContext && activeMeasurements.TryGetValue(dbContext, out var metrics))
        {
            metrics.ColumnCount = result.FieldCount;
        }

        return ValueTask.FromResult(result);
    }

    public override InterceptionResult DataReaderDisposing(
        DbCommand command,
        DataReaderDisposingEventData eventData,
        InterceptionResult result)
    {
        if (eventData.Context is { } dbContext && activeMeasurements.TryGetValue(dbContext, out var metrics))
        {
            // The measured queries fully enumerate one reader. EF counts the final read that returns false.
            metrics.RowCount = eventData.ReadCount - 1;
        }

        return result;
    }

    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (activeMeasurements.TryGetValue(materializationData.Context, out var metrics))
        {
            metrics.MaterializedEntityCount++;
        }

        return entity;
    }
}

internal sealed class QueryShapeMetrics
{
    public int? RowCount { get; set; }
    public int? ColumnCount { get; set; }
    public int MaterializedEntityCount { get; set; }
}
