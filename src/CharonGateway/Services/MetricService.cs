using CharonGateway.Models.DTOs;
using CharonGateway.Models.Requests;
using CharonGateway.Repositories.Interfaces;
using CharonGateway.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CharonGateway.Services;

public class MetricService : IMetricService
{
    private readonly IMetricRepository _repository;

    public MetricService(IMetricRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<MetricDto?> GetMetricByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var metric = await _repository.GetByIdAsync(id, cancellationToken);
        return metric == null ? null : MapToDto(metric);
    }

    public async Task<PagedResult<MetricDto>> GetMetricsAsync(MetricQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Apply filters
        query = ApplyFilters(query, request);

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var metrics = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var metricDtos = metrics.Select(MapToDto).ToList();

        return new PagedResult<MetricDto>
        {
            Items = metricDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<IEnumerable<MetricDto>> GetMetricsByTypeAsync(string type, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var metrics = await _repository.GetByTypeAsync(type, cancellationToken);
        
        var pagedMetrics = metrics
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return pagedMetrics.Select(MapToDto);
    }

    public async Task<MetricsAggregationDto> GetMetricsAggregationAsync(MetricAggregationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Apply date filters
        if (request.FromDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= request.ToDate.Value);
        }

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(m => m.Type == request.Type);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var typeGroups = await query
            .GroupBy(m => m.Type)
            .Select(g => new TypeAggregationDto
            {
                Type = g.Key ?? string.Empty,
                Count = g.Count()
            })
            .OrderByDescending(ta => ta.Count)
            .ToListAsync(cancellationToken);

        return new MetricsAggregationDto
        {
            TotalCount = totalCount,
            TypeAggregations = typeGroups
        };
    }

    public async Task<IEnumerable<string>> GetMetricTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetDistinctTypesAsync(cancellationToken);
    }

    public async Task<IEnumerable<DailyAverageMetricDto>> GetDailyAveragesAsync(
        DateTime fromDate,
        DateTime toDate,
        string? type = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        // Normalize dates to ensure proper filtering (start of fromDate, end of toDate)
        // Use Date property to compare dates without time component
        var normalizedFromDate = fromDate.Date;
        var normalizedToDate = toDate.Date;
        
        // Limit date range to maximum 30 days to avoid complexity issues
        var maxDays = 30;
        if ((normalizedToDate - normalizedFromDate).TotalDays > maxDays)
        {
            normalizedToDate = normalizedFromDate.AddDays(maxDays);
        }
        
        var query = _repository.GetQueryable()
            .Where(m => m.CreatedAt.Date >= normalizedFromDate && m.CreatedAt.Date <= normalizedToDate);

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(m => m.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(m => m.Name.Contains(name));
        }

        // Group by date (without time), type, and name
        var groupedMetrics = await query
            .ToListAsync(cancellationToken);

        var dailyGroups = groupedMetrics
            .GroupBy(m => new
            {
                Date = m.CreatedAt.Date,
                Type = m.Type,
                Name = m.Name
            })
            .Select(g => new DailyAverageMetricDto
            {
                Date = g.Key.Date,
                Type = g.Key.Type,
                Name = g.Key.Name,
                Count = g.Count(),
                AverageValues = CalculateAverageValues(g.Select(m => m.PayloadJson).ToList())
            })
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Type)
            .ThenBy(d => d.Name)
            .ToList();

        return dailyGroups;
    }

    private Dictionary<string, double> CalculateAverageValues(List<string> payloadJsonList)
    {
        var result = new Dictionary<string, double>();
        var numericValues = new Dictionary<string, List<double>>();

        if (payloadJsonList == null || payloadJsonList.Count == 0)
            return result;

        foreach (var payloadJson in payloadJsonList)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                continue;

            try
            {
                // Use JsonDocument for more flexible parsing
                using var doc = JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var property in root.EnumerateObject())
                {
                    var value = ConvertJsonElementToDouble(property.Value);
                    if (value.HasValue)
                    {
                        if (!numericValues.ContainsKey(property.Name))
                        {
                            numericValues[property.Name] = new List<double>();
                        }
                        numericValues[property.Name].Add(value.Value);
                    }
                }
            }
            catch (JsonException)
            {
                // Skip invalid JSON
                continue;
            }
            catch
            {
                // Skip invalid payloads
                continue;
            }
        }

        // Calculate averages and limit to top 5 most common keys to reduce complexity
        var sortedKeys = numericValues
            .OrderByDescending(kvp => kvp.Value.Count)
            .Take(5)
            .ToList();

        foreach (var kvp in sortedKeys)
        {
            if (kvp.Value.Count > 0)
            {
                result[kvp.Key] = kvp.Value.Average();
            }
        }

        return result;
    }

    private double? ConvertJsonElementToDouble(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => double.TryParse(element.GetString(), out var parsed) ? parsed : null,
            JsonValueKind.True => 1.0,
            JsonValueKind.False => 0.0,
            _ => null
        };
    }

    private double? ConvertToDouble(object? value)
    {
        if (value == null)
            return null;

        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal dec => (double)dec,
            string str => double.TryParse(str, out var parsed) ? parsed : null,
            _ => null
        };
    }

    private IQueryable<CharonDbContext.Models.Metric> ApplyFilters(
        IQueryable<CharonDbContext.Models.Metric> query,
        MetricQueryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(m => m.Type == request.Type);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(m => m.Name.Contains(request.Name));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= request.ToDate.Value);
        }

        return query;
    }

    private IQueryable<CharonDbContext.Models.Metric> ApplySorting(
        IQueryable<CharonDbContext.Models.Metric> query,
        string? sortBy,
        string? sortOrder)
    {
        var orderBy = sortBy?.ToLower() ?? "createdat";
        var isAscending = sortOrder?.ToLower() == "asc";

        return orderBy switch
        {
            "type" => isAscending ? query.OrderBy(m => m.Type) : query.OrderByDescending(m => m.Type),
            "name" => isAscending ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
            "createdat" => isAscending ? query.OrderBy(m => m.CreatedAt) : query.OrderByDescending(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.CreatedAt)
        };
    }

    private MetricDto MapToDto(CharonDbContext.Models.Metric metric)
    {
        Dictionary<string, object> payload = new();

        if (!string.IsNullOrEmpty(metric.PayloadJson))
        {
            try
            {
                payload = JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson)
                    ?? new Dictionary<string, object>();
            }
            catch (JsonException)
            {
                // Invalid JSON payload - return empty dictionary
                // Exception handling and logging is done by the decorator
            }
        }

        return new MetricDto
        {
            Id = metric.Id,
            Type = metric.Type,
            Name = metric.Name,
            Payload = payload,
            CreatedAt = metric.CreatedAt
        };
    }
}


