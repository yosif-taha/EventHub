using EventHub.Application.Common.Dtos.Registrations.Payments;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Payments
{
    public record ProcessPaymobWebhookCommand(PaymobTransactionObj Payload) : IRequest<RequestResult<bool>>;
}
