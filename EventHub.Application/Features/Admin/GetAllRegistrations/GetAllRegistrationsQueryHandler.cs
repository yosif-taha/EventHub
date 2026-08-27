using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Admin.GetAllRegistrations
{
    public class GetAllRegistrationsQueryHandler(
        IGenericRepository<Registration> _registrationRepository,
        IUserContext _userContext,
        IMapper _mapper,
        IDbExecutor _executor) : IRequestHandler<GetAllRegistrationsQuery, RequestResult<PaginatedList<AdminRegistrationDto>>>
    {
        public async Task<RequestResult<PaginatedList<AdminRegistrationDto>>> Handle(
            GetAllRegistrationsQuery request,
            CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Admin))
                return RequestResult<PaginatedList<AdminRegistrationDto>>.Failure(ErrorCode.Forbidden);

            var registrations = _registrationRepository.GetAll()
                .OrderByDescending(registration => registration.RegistrationDate);

            var result = await _executor.CreateAsync<Registration, AdminRegistrationDto>(
                registrations,
                request.PageNumber,
                request.PageSize,
                _mapper.ConfigurationProvider,
                cancellationToken);

            return RequestResult<PaginatedList<AdminRegistrationDto>>.Success(result);
        }
    }
}
