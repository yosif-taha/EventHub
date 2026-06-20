using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventHub.Application.Common.Models;
using EventHub.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace EventHub.Infrastructure.Common
{
    public class DbExecutor : IDbExecutor
    {
        public async Task<PaginatedList<TDestination>> CreateAsync<TSource, TDestination>(IQueryable<TSource> source, int pageNumber, int pageSize, IConfigurationProvider configuration, CancellationToken ct = default)
        {
            var count = await source.CountAsync(ct);

            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<TDestination>(configuration)
                .ToListAsync(ct);

            return new PaginatedList<TDestination>(items, count, pageNumber, pageSize);
        }

        public async Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken ct = default)
        {
            return await query.ToListAsync(ct); 
        }
        public async Task<TResult> FirstOrDefaultAsync<TResult>(IQueryable<TResult> query, Expression<Func<TResult, bool>> expression, CancellationToken ct = default)
        {
            return await query.FirstOrDefaultAsync(expression,ct);
        }
    }
}
