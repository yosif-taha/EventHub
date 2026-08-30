using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Events.GetManagedEventById;

public sealed class GetManagedEventByIdQueryHandler(
    IGenericRepository<Event> eventRepository,
    IUserContext userContext,
    IMapper mapper) : IRequestHandler<GetManagedEventByIdQuery, RequestResult<EventDto>>
{
    public async Task<RequestResult<EventDto>> Handle(GetManagedEventByIdQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = userContext.IsInRole(RoleNames.Admin);
        var isOrganizer = userContext.IsInRole(RoleNames.Organizer);
        if (!isAdmin && !isOrganizer)
            return RequestResult<EventDto>.Failure(ErrorCode.Forbidden);

        var events = eventRepository.GetAll()
            .AsNoTracking()
            .Where(@event => @event.Id == request.EventId);

        if (!isAdmin)
            events = events.Where(@event => @event.OrganizerId == userContext.UserId);

        var @event = await events
            .ProjectTo<EventDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return @event is null
            ? RequestResult<EventDto>.Failure(ErrorCode.EventNotFound)
            : RequestResult<EventDto>.Success(@event);
    }
}
