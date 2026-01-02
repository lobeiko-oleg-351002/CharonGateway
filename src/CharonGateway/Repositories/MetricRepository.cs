using CharonDbContext.Data;
using CharonDbContext.Models;
using CharonGateway.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CharonGateway.Repositories;

/// <summary>
/// Repository for accessing metrics from the database.
/// Exception handling is done by GlobalExceptionHandlerMiddleware at the HTTP level.
/// </summary>
public class MetricRepository : IMetricRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MetricRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Metric?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Metric>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metrics
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Metric>> FindAsync(Expression<Func<Metric, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metrics
            .AsNoTracking()
            .Where(predicate)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Metric>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metrics
            .AsNoTracking()
            .Where(m => m.Type == type)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<Metric, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Metrics.AsNoTracking();
        
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetDistinctTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metrics
            .AsNoTracking()
            .Select(m => m.Type)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<Metric> GetQueryable()
    {
        return _dbContext.Metrics.AsNoTracking();
    }
}


