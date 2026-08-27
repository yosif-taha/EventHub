using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;
using System.Linq.Dynamic.Core;

namespace EventHub.Application.Features.Events.GetMyEvents
{
    public class GetMyEventsQueryHandler(
        IGenericRepository<Event> _eventRepository,
        IUserContext _userContext,
        IMapper _mapper,
        IDbExecutor _executor) : IRequestHandler<GetMyEventsQuery, RequestResult<PaginatedList<EventDto>>>
    {
        public async Task<RequestResult<PaginatedList<EventDto>>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Organizer))
                return RequestResult<PaginatedList<EventDto>>.Failure(ErrorCode.Forbidden);

            var events = _eventRepository.GetAll()
                .Where(@event => @event.OrganizerId == _userContext.UserId);

            if (request.CategoryId.HasValue)
                events = events.Where(@event => @event.CategoryId == request.CategoryId);

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
                events = events.Where(@event => @event.Title.Contains(request.SearchValue) || @event.Location.Contains(request.SearchValue));

            if (!string.IsNullOrWhiteSpace(request.SortColumn))
                events = events.OrderBy($"{request.SortColumn} {request.SortDirection}");
            else
                events = events.OrderByDescending(@event => @event.EventDate);

            var result = await _executor.CreateAsync<Event, EventDto>(
                events,
                request.PageNumber,
                request.PageSize,
                _mapper.ConfigurationProvider,
                cancellationToken);

            return RequestResult<PaginatedList<EventDto>>.Success(result);
        }
    }
}
