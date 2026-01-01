using CharonDbContext.Models;
using System.Linq.Expressions;

namespace CharonGateway.Repositories.Interfaces;

public interface IMetricRepository
{
    Task<Metric?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Metric>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Metric>> FindAsync(Expression<Func<Metric, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Metric>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Metric, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetDistinctTypesAsync(CancellationToken cancellationToken = default);
    IQueryable<Metric> GetQueryable();
}


