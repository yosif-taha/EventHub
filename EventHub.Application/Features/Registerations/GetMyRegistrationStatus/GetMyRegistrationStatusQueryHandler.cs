using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Registerations.GetMyRegistrationStatus;

public sealed class GetMyRegistrationStatusQueryHandler(
    IGenericRepository<Registration> registrationRepository,
    IUserContext userContext,
    IMapper mapper) : IRequestHandler<GetMyRegistrationStatusQuery, RequestResult<RegistrationPaymentStatusDto>>
{
    public async Task<RequestResult<RegistrationPaymentStatusDto>> Handle(
        GetMyRegistrationStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.Attendee))
            return RequestResult<RegistrationPaymentStatusDto>.Failure(ErrorCode.Forbidden);

        var registration = await registrationRepository.GetByIdProjectedAsync<RegistrationPaymentStatusDto>(
            item => item.Id == request.RegistrationId && item.UserId == userContext.UserId,
            mapper.ConfigurationProvider,
            cancellationToken);

        return registration is null
            ? RequestResult<RegistrationPaymentStatusDto>.Failure(ErrorCode.RegistrationNotFound)
            : RequestResult<RegistrationPaymentStatusDto>.Success(registration);
    }
}
