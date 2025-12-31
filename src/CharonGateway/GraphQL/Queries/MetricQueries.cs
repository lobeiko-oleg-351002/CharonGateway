using CharonDbContext.Data;
using CharonDbContext.Models;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharonGateway.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class MetricQueries
{
    private readonly ILogger<MetricQueries> _logger;

    public MetricQueries(ILogger<MetricQueries> logger)
    {
        _logger = logger;
    }

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Metric> GetMetrics(
        [Service] ApplicationDbContext? context)
    {
        try
        {
            if (context == null)
            {
                _logger?.LogWarning("ApplicationDbContext is null in GetMetrics");
                return Array.Empty<Metric>().AsQueryable();
            }
            return context.Metrics;
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Metrics table does not exist yet. Database may not be initialized.");
            return Array.Empty<Metric>().AsQueryable();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error accessing Metrics in GetMetrics: {Error}", ex.Message);
            return Array.Empty<Metric>().AsQueryable();
        }
    }

    public async Task<Metric?> GetMetricById(
        int id,
        [Service] ApplicationDbContext context)
    {
        if (context == null)
        {
            _logger?.LogWarning("ApplicationDbContext is null in GetMetricById");
            return null;
        }
        return await context.Metrics.FirstOrDefaultAsync(m => m.Id == id);
    }

    [UseFiltering]
    [UseSorting]
    public IQueryable<Metric> GetMetricsByType(
        string type,
        [Service] ApplicationDbContext context)
    {
        if (context == null)
        {
            _logger?.LogWarning("ApplicationDbContext is null in GetMetricsByType");
            return Array.Empty<Metric>().AsQueryable();
        }
        return context.Metrics.Where(m => m.Type == type);
    }

    public async Task<MetricsAggregation> GetMetricsAggregation(
        [Service] ApplicationDbContext context,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? type = null)
    {
        if (context == null)
        {
            _logger.LogWarning("ApplicationDbContext is null in GetMetricsAggregation");
            return new MetricsAggregation
            {
                TotalCount = 0,
                TypeAggregations = new List<TypeAggregation>()
            };
        }

        try
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
                    Type = g.Key ?? string.Empty,
                    Count = g.Count()
                })
                .ToListAsync();

            return new MetricsAggregation
            {
                TotalCount = totalCount,
                TypeAggregations = typeGroups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMetricsAggregation");
            return new MetricsAggregation
            {
                TotalCount = 0,
                TypeAggregations = new List<TypeAggregation>()
            };
        }
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

