using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Registerations.GetMyRegistrationStatus;

public record GetMyRegistrationStatusQuery(Guid RegistrationId) : IRequest<RequestResult<RegistrationPaymentStatusDto>>;
