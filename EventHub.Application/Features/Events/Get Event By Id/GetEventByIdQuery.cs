using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Events.Get_Event_By_Id
{
    public record GetEventByIdQuery(
        Guid Id) : IRequest<RequestResult<EventDto>>;
 
}
