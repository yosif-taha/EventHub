using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventHub.Application.Contracts;
using EventHub.Domin.Common;
using EventHub.Persistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace EventHub.Persistence.Reposetories
{
    public class GenericRepository<T>(EventDbContext context) : IGenericRepository<T> where T : BaseModel
    {
        protected readonly EventDbContext _context = context;
        private readonly DbSet<T> _dbSet = context.Set<T>();

        public IQueryable<T> GetAll() => _dbSet.AsQueryable();

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) => await _dbSet.FirstOrDefaultAsync(t => t.Id == id, ct);
        public async Task<T?> GetByIdAsTrackingAsync(Guid id, CancellationToken ct) => await _dbSet.AsTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        public async Task<TResult?> GetByIdProjectedAsync<TResult>(Expression<Func<T, bool>> predicate, IConfigurationProvider configuration, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Where(predicate)
                .ProjectTo<TResult>(configuration) 
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken) => await _dbSet.AddAsync(entity, cancellationToken);

        public void SaveInclude(T entity, params string[] includeProperties) // Update only the included properties of the entity
        {
            var localEntity = _context.Set<T>().Local.FirstOrDefault(e => e.Id == entity.Id);
            EntityEntry<T> entry;
            if (localEntity == null)
            {
                _context.Set<T>().Attach(entity);
                entry = _context.Entry(entity);
            }
            else
            { 
              entry = _context.Entry(localEntity);
            }
            foreach(var includeProperty in includeProperties)
            {
                var value = entity.GetType().GetProperty(includeProperty)?.GetValue(entity);
                entry.Property(includeProperty).CurrentValue = value;
                entry.Property(includeProperty).IsModified = true;
            }
        }
        public void SoftDelete(T entity)
        {
            var localEntity = _context.Set<T>().Local.FirstOrDefault(e => e.Id == entity.Id);
            EntityEntry<T> entry;
            if (localEntity == null)
            {
                _context.Set<T>().Attach(entity);
                entry = _context.Entry(entity);
            }
            else
            {
                entry = _context.Entry(localEntity);
            }
            entity.IsDeleted = true;
            entry.Property(x => x.IsDeleted).IsModified = true;
        }
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) => await _context.Set<T>().AnyAsync(predicate, cancellationToken);

    }
}
