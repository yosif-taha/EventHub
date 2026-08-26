using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;
using MediatR;

namespace EventHub.Application.Features.Events.Update_Event_Status
{
    public record UpdateEventStatusCommand(Guid Id, EventStatus Status) : IRequest<RequestResult<Unit>>;
}
