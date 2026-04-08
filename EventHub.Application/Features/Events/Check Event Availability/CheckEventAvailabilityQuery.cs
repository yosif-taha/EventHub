using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Events.Check_Event_Availability
{
    public record CheckEventAvailabilityQuery(Guid Id) : IRequest<RequestResult<EventAvailabilityDto>>;
}
