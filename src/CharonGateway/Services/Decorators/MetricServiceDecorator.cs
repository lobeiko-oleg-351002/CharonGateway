using CharonGateway.Middleware.Interfaces;
using CharonGateway.Models.DTOs;
using CharonGateway.Models.Requests;
using CharonGateway.Services.Interfaces;

namespace CharonGateway.Services.Decorators;

public class MetricServiceDecorator : IMetricService
{
    private readonly IMetricService _inner;
    private readonly IExceptionHandlingService _exceptionHandling;

    public MetricServiceDecorator(
        IMetricService inner,
        IExceptionHandlingService exceptionHandling)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _exceptionHandling = exceptionHandling ?? throw new ArgumentNullException(nameof(exceptionHandling));
    }

    public async Task<MetricDto?> GetMetricByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _exceptionHandling.ExecuteAsync(
            async () => await _inner.GetMetricByIdAsync(id, cancellationToken),
            $"{nameof(GetMetricByIdAsync)} (Id: {id})",
            cancellationToken);
    }

    public async Task<PagedResult<MetricDto>> GetMetricsAsync(MetricQueryRequest request, CancellationToken cancellationToken = default)
    {
        return await _exceptionHandling.ExecuteAsync(
            async () => await _inner.GetMetricsAsync(request, cancellationToken),
            $"{nameof(GetMetricsAsync)} (Page: {request?.Page}, PageSize: {request?.PageSize})",
            cancellationToken);
    }

    public async Task<IEnumerable<MetricDto>> GetMetricsByTypeAsync(string type, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _exceptionHandling.ExecuteAsync(
            async () => await _inner.GetMetricsByTypeAsync(type, page, pageSize, cancellationToken),
            $"{nameof(GetMetricsByTypeAsync)} (Type: {type}, Page: {page})",
            cancellationToken);
    }

    public async Task<MetricsAggregationDto> GetMetricsAggregationAsync(MetricAggregationRequest request, CancellationToken cancellationToken = default)
    {
        return await _exceptionHandling.ExecuteAsync(
            async () => await _inner.GetMetricsAggregationAsync(request, cancellationToken),
            $"{nameof(GetMetricsAggregationAsync)}",
            cancellationToken);
    }

    public async Task<IEnumerable<string>> GetMetricTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _exceptionHandling.ExecuteAsync(
            async () => await _inner.GetMetricTypesAsync(cancellationToken),
            nameof(GetMetricTypesAsync),
            cancellationToken);
    }
}


