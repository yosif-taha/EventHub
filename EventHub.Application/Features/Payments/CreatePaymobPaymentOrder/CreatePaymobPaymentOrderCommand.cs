using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Payments.CreatePaymobPaymentOrder;

public sealed record CreatePaymobPaymentOrderCommand(Guid RegistrationId)
    : IRequest<RequestResult<PaymobPaymentOrderResultDto>>;

public sealed record PaymobPaymentOrderResultDto(Guid RegistrationId, string? PaymentUrl);
