using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Admin.UpdateUserRole
{
    public class UpdateUserRoleCommandHandler(IUserManagementService _userManagementService, IUserContext _userContext)
        : IRequestHandler<UpdateUserRoleCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(EventHub.Domin.Constants.RoleNames.Admin))
                return RequestResult<bool>.Failure(ErrorCode.Forbidden);

            return await _userManagementService.UpdateUserRoleAsync(request.UserId, request.Role, cancellationToken);
        }
    }
}
