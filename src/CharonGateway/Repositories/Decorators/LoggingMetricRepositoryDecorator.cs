using CharonDbContext.Models;
using CharonGateway.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CharonGateway.Repositories.Decorators;

/// <summary>
/// Decorator for MetricRepository that adds logging for method calls.
/// Exceptions are not handled here - they propagate to GlobalExceptionHandlerMiddleware.
/// </summary>
public class LoggingMetricRepositoryDecorator : IMetricRepository
{
    private readonly IMetricRepository _inner;
    private readonly ILogger<LoggingMetricRepositoryDecorator> _logger;

    public LoggingMetricRepositoryDecorator(
        IMetricRepository inner,
        ILogger<LoggingMetricRepositoryDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Metric?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metric by id: {MetricId}", id);
        var result = await _inner.GetByIdAsync(id, cancellationToken);
        _logger.LogDebug("Retrieved metric by id: {MetricId}, Found: {Found}", id, result != null);
        return result;
    }

    public async Task<IEnumerable<Metric>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all metrics");
        var result = await _inner.GetAllAsync(cancellationToken);
        _logger.LogDebug("Retrieved {Count} metrics", result.Count());
        return result;
    }

    public async Task<IEnumerable<Metric>> FindAsync(Expression<Func<Metric, bool>> predicate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding metrics with predicate");
        var result = await _inner.FindAsync(predicate, cancellationToken);
        _logger.LogDebug("Found {Count} metrics matching predicate", result.Count());
        return result;
    }

    public async Task<IEnumerable<Metric>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metrics by type: {Type}", type);
        var result = await _inner.GetByTypeAsync(type, cancellationToken);
        _logger.LogDebug("Retrieved {Count} metrics of type {Type}", result.Count(), type);
        return result;
    }

    public async Task<int> CountAsync(Expression<Func<Metric, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting metrics, HasPredicate: {HasPredicate}", predicate != null);
        var result = await _inner.CountAsync(predicate, cancellationToken);
        _logger.LogDebug("Count result: {Count}", result);
        return result;
    }

    public async Task<IEnumerable<string>> GetDistinctTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting distinct metric types");
        var result = await _inner.GetDistinctTypesAsync(cancellationToken);
        _logger.LogDebug("Retrieved {Count} distinct types", result.Count());
        return result;
    }

    public IQueryable<Metric> GetQueryable()
    {
        _logger.LogDebug("Getting queryable metrics");
        return _inner.GetQueryable();
    }
}



