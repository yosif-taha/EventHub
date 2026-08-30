using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Admin.GetUsers
{
    public class GetUsersQueryHandler(IUserManagementService _userManagementService, IUserContext _userContext)
        : IRequestHandler<GetUsersQuery, RequestResult<PaginatedList<UserRoleDto>>>
    {
        public async Task<RequestResult<PaginatedList<UserRoleDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(EventHub.Domin.Constants.RoleNames.Admin))
                return RequestResult<PaginatedList<UserRoleDto>>.Failure(ErrorCode.Forbidden);

            return await _userManagementService.GetUsersAsync(request.PageNumber, request.PageSize, request.SearchValue, cancellationToken);
        }
    }
}
