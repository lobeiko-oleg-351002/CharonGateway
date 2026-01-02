using CharonGateway.Models.DTOs;
using CharonGateway.Models.Requests;
using CharonGateway.Services.Interfaces;
using System.Reflection;

namespace CharonGateway.Services.Decorators;

public class ValidationDecorator : IMetricService
{
    private readonly IMetricService _inner;

    public ValidationDecorator(IMetricService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<MetricDto?> GetMetricByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        return await _inner.GetMetricByIdAsync(id, cancellationToken);
    }

    public async Task<PagedResult<MetricDto>> GetMetricsAsync(MetricQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // FluentValidation handles request validation, but we ensure request is not null
        return await _inner.GetMetricsAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<MetricDto>> GetMetricsByTypeAsync(string type, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidateType(type);
        ValidatePagination(page, pageSize);
        
        return await _inner.GetMetricsByTypeAsync(type, page, pageSize, cancellationToken);
    }

    public async Task<MetricsAggregationDto> GetMetricsAggregationAsync(MetricAggregationRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // FluentValidation handles request validation, but we ensure request is not null
        return await _inner.GetMetricsAggregationAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetMetricTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _inner.GetMetricTypesAsync(cancellationToken);
    }

    public async Task<IEnumerable<DailyAverageMetricDto>> GetDailyAveragesAsync(
        DateTime fromDate,
        DateTime toDate,
        string? type = null,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            throw new ArgumentException("FromDate cannot be greater than ToDate", nameof(fromDate));
        }

        return await _inner.GetDailyAveragesAsync(fromDate, toDate, type, name, cancellationToken);
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Metric ID must be greater than zero", nameof(id));
        }
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or empty", nameof(type));
        }
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
        }
    }
}

