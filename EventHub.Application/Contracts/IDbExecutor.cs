using AutoMapper;
using EventHub.Application.Common.Models;

namespace EventHub.Application.Contracts
{

    public interface IDbExecutor
    {
        Task<PaginatedList<TDestination>> CreateAsync<TSource, TDestination>(
          IQueryable<TSource> source,
          int pageNumber,
          int pageSize,
          IConfigurationProvider configuration, 
          CancellationToken ct = default);
        Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken ct = default);
    }

}
