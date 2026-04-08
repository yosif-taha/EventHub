using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Features.Account.ChangePassword;
using EventHub.Application.Features.Account.GetUserProfile;
using EventHub.Application.Features.Account.UpdateUserProfile;
using EventHub.WebAPI.Presentation.ViewModels.Account;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class AccountController(IMediator _mediator, IMapper _mapper) : ControllerBase
    {
        [HttpGet]
        public async Task<ResponseViewModel> GetUserProfile([FromQuery] GetUserProfileRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetUserProfileQuery(request.UserId), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            var response = _mapper.Map<AccountResponseViewModel>(result.Data);
            return new SuccessResponseViewModelT<AccountResponseViewModel>(response);
        }

        [HttpPost]
        public async Task<ResponseViewModel> UpdateUserProfile([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new UpdateUserProfileCommand(request.UserId, request.FullName),ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Your profile has been updated successfully.");
        }

        [HttpPost]
        public async Task<ResponseViewModel> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new ChangePasswordCommand(request.UserId,request.CurrentPassword,request.NewPassword), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Your password has been changed successfully.");
        }

    }
}
