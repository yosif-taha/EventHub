using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Registerations.RegisterationForEvent
{
    public record RegisterationCommand(Guid EventId) : IRequest<RequestResult<Guid>>;
}
