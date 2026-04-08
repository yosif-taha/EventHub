using AutoMapper;
using EventHub.Domin.Common;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EventHub.Application.Contracts
{
    public interface IGenericRepository<T> where T : BaseModel
    {
        IQueryable<T> GetAll();
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<TResult?> GetByIdProjectedAsync<TResult>(Expression<Func<T, bool>> predicate, IConfigurationProvider configuration, CancellationToken cancellationToken);
        Task AddAsync(T entity, CancellationToken cancellationToken);
        Task UpdateAsync(Expression<Func<T, bool>> predicate,
          Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> updateExpression, CancellationToken cancellationToken);
        Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}
