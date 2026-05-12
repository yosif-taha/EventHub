using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;
using System.Linq.Dynamic.Core;

namespace EventHub.Application.Features.Events.GetAll_Events
{
    public class GetAllEventsQueryHandler(IGenericRepository<Event> _repository,
        IMapper _mapper,
        IDbExecutor _pagination) : IRequestHandler<GetAllEventsQuery, RequestResult<PaginatedList<EventDto>>>
    {
        public async Task<RequestResult<PaginatedList<EventDto>>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
        {
            var query = _repository.GetAll();

            if (request.CategoryId.HasValue)
                query = query.Where(e => e.CategoryId == request.CategoryId);

            if (!string.IsNullOrEmpty(request.SearchValue))
            {
                query = query.Where(e => e.Title.Contains(request.SearchValue) || e.Location.Contains(request.SearchValue));
            }

            if (!string.IsNullOrEmpty(request.SortColumn))
            {
                var sortExpression = $"{request.SortColumn} {request.SortDirection}";
                query = query.OrderBy(sortExpression);
            }

            var result = await _pagination.CreateAsync<Event,EventDto>(query, request.PageNumber,request.PageSize,_mapper.ConfigurationProvider,cancellationToken);

            return RequestResult<PaginatedList<EventDto>>.Success(result);
        }
    }
}
