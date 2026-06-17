using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Registerations.GetUserRegistrations
{
    public class GetMyRegistrationsQueryHandler(
            IGenericRepository<Registration> _registrationRepository,
            IUserContext _userContext,
            IMapper _mapper,
            IDbExecutor _executor
        ) : IRequestHandler<GetMyRegistrationsQuery, RequestResult<PaginatedList<UserRegistrationDto>>>
    {
        public async Task<RequestResult<PaginatedList<UserRegistrationDto>>> Handle(
            GetMyRegistrationsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var query = _registrationRepository.GetAll();

                query = query.Where(r => r.UserId == _userContext.UserId);

                var result = await _executor.CreateAsync<Registration,UserRegistrationDto>(query,request.PageNumber,request.PageSize,_mapper.ConfigurationProvider,cancellationToken); 

                return RequestResult<PaginatedList<UserRegistrationDto>>.Success(result);
            }
            catch (Exception)
            {
                return RequestResult<PaginatedList<UserRegistrationDto>>.Failure(
                    ErrorCode.InternalServerError,
                    "An error occurred while retrieving your registrations."
                );
            }
        }
    }
}
