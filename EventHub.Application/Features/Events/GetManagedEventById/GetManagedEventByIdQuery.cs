using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Events.GetManagedEventById;

public sealed record GetManagedEventByIdQuery(Guid EventId) : IRequest<RequestResult<EventDto>>;
