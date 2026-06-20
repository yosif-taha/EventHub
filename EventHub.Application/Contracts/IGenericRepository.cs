using AutoMapper;
using EventHub.Domin.Common;
using System.Linq.Expressions;

namespace EventHub.Application.Contracts
{
    public interface IGenericRepository<T> where T : BaseModel
    {
        IQueryable<T> GetAll();
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<T?> GetByIdAsTrackingAsync(Guid id, CancellationToken cancellationToken);
        Task<TResult?> GetByIdProjectedAsync<TResult>(Expression<Func<T, bool>> predicate, IConfigurationProvider configuration, CancellationToken cancellationToken);
        Task AddAsync(T entity, CancellationToken cancellationToken);
        void SaveInclude(T entity, params string[] includeProperties);
        void SoftDelete(T entity);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
    }
}
