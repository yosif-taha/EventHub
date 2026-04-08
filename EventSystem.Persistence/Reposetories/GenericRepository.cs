using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventHub.Application.Contracts;
using EventHub.Domin.Common;
using EventHub.Persistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EventHub.Persistence.Reposetories
{
    public class GenericRepository<T>(EventDbContext context) : IGenericRepository<T> where T : BaseModel
    {
        protected readonly EventDbContext _context = context;
        private readonly DbSet<T> _dbSet = context.Set<T>();

        public IQueryable<T> GetAll() => _dbSet.AsQueryable();

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) => await _dbSet.FirstOrDefaultAsync(t => t.Id == id, ct);
        public async Task<TResult?> GetByIdProjectedAsync<TResult>(Expression<Func<T, bool>> predicate, IConfigurationProvider configuration, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Where(predicate)
                .ProjectTo<TResult>(configuration) 
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken) => await _dbSet.AddAsync(entity, cancellationToken);

        public async Task UpdateAsync(Expression<Func<T, bool>> predicate,
          Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> updateExpression, CancellationToken cancellationToken)
        {
            await _dbSet.Where(predicate).ExecuteUpdateAsync(updateExpression, cancellationToken);
        }

        public async Task SoftDeleteAsync(Guid id, CancellationToken ct)
        {
            await _dbSet
                .Where(e => EF.Property<Guid>(e, "Id") == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => EF.Property<bool>(e, "IsDeleted"), true), ct);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => await _context.Set<T>().AnyAsync(predicate);
    }
}
