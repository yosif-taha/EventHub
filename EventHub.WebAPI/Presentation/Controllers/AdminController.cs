using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Features.Admin.GetUsers;
using EventHub.Application.Features.Admin.UpdateUserRole;
using EventHub.WebAPI.Presentation.ViewModels.Admin;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using EventHub.Domin.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController(IMediator _mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ResponseViewModel> GetUsers(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetUsersQuery(), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());

            return new SuccessResponseViewModelT<IReadOnlyList<UserRoleDto>>(result.Data!);
        }

        [HttpPost("{userId}")]
        public async Task<ResponseViewModel> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new UpdateUserRoleCommand(userId, request.Role), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());

            return new SuccessResponseViewModel("User role updated successfully.");
        }
    }
}
