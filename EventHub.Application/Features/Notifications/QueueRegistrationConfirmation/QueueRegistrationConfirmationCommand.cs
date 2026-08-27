using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Notifications.QueueRegistrationConfirmation
{
    public record QueueRegistrationConfirmationCommand(Guid RegistrationId) : IRequest<RequestResult<bool>>;
}
