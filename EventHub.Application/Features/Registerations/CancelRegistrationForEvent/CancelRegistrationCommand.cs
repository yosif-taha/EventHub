using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Registerations.CancelRegistrationForEvent
{
    public record CancelRegistrationCommand(Guid RegistrationId) : IRequest<RequestResult<bool>>;
}
