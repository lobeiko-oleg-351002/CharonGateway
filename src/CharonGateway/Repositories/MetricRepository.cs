using CharonDbContext.Data;
using CharonDbContext.Models;
using CharonGateway.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CharonGateway.Repositories;

public class MetricRepository : IMetricRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MetricRepository> _logger;

    public MetricRepository(
        ApplicationDbContext dbContext,
        ILogger<MetricRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Metric?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Metrics
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metric with id {MetricId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Metric>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Metrics
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all metrics");
            throw;
        }
    }

    public async Task<IEnumerable<Metric>> FindAsync(Expression<Func<Metric, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Metrics
                .AsNoTracking()
                .Where(predicate)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding metrics with predicate");
            throw;
        }
    }

    public async Task<IEnumerable<Metric>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        // Validation is handled by ValidationDecorator in the service layer
        return await _dbContext.Metrics
            .AsNoTracking()
            .Where(m => m.Type == type)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<Metric, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.Metrics.AsNoTracking();
            
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting metrics");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetDistinctTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Metrics
                .AsNoTracking()
                .Select(m => m.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving distinct metric types");
            throw;
        }
    }

    public IQueryable<Metric> GetQueryable()
    {
        return _dbContext.Metrics.AsNoTracking();
    }
}


