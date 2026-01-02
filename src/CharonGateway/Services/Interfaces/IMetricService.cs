using CharonGateway.Models.DTOs;
using CharonGateway.Models.Requests;

namespace CharonGateway.Services.Interfaces;

public interface IMetricService
{
    Task<MetricDto?> GetMetricByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<MetricDto>> GetMetricsAsync(MetricQueryRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<MetricDto>> GetMetricsByTypeAsync(string type, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MetricsAggregationDto> GetMetricsAggregationAsync(MetricAggregationRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetMetricTypesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DailyAverageMetricDto>> GetDailyAveragesAsync(DateTime fromDate, DateTime toDate, string? type = null, string? name = null, CancellationToken cancellationToken = default);
}


