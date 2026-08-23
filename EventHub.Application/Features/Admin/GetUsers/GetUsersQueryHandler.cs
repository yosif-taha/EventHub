using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Admin.GetUsers
{
    public class GetUsersQueryHandler(IUserManagementService _userManagementService, IUserContext _userContext)
        : IRequestHandler<GetUsersQuery, RequestResult<IReadOnlyList<UserRoleDto>>>
    {
        public async Task<RequestResult<IReadOnlyList<UserRoleDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(EventHub.Domin.Constants.RoleNames.Admin))
                return RequestResult<IReadOnlyList<UserRoleDto>>.Failure(ErrorCode.Forbidden);

            return await _userManagementService.GetUsersAsync(cancellationToken);
        }
    }
}
