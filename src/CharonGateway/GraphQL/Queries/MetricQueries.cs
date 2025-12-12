using CharonGateway.Data;
using CharonGateway.Models;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace CharonGateway.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class MetricQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Metric> GetMetrics(
        [Service] ApplicationDbContext context)
    {
        return context.Metrics;
    }

    public async Task<Metric?> GetMetricById(
        int id,
        [Service] ApplicationDbContext context)
    {
        return await context.Metrics.FirstOrDefaultAsync(m => m.Id == id);
    }

    [UseFiltering]
    [UseSorting]
    public IQueryable<Metric> GetMetricsByType(
        string type,
        [Service] ApplicationDbContext context)
    {
        return context.Metrics.Where(m => m.Type == type);
    }

    public async Task<MetricsAggregation> GetMetricsAggregation(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? type = null,
        [Service] ApplicationDbContext context = null!)
    {
        var query = context.Metrics.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(m => m.Type == type);
        }

        var totalCount = await query.CountAsync();
        var typeGroups = await query
            .GroupBy(m => m.Type)
            .Select(g => new TypeAggregation
            {
                Type = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return new MetricsAggregation
        {
            TotalCount = totalCount,
            TypeAggregations = typeGroups
        };
    }
}

public class MetricsAggregation
{
    public int TotalCount { get; set; }
    public List<TypeAggregation> TypeAggregations { get; set; } = new();
}

public class TypeAggregation
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

