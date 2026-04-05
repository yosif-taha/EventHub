using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Features.Account.ChangePassword;
using EventHub.Application.Features.Account.GetUserProfile;
using EventHub.Application.Features.Account.UpdateUserProfile;
using EventHub.WebAPI.Presentation.ViewModels.Account;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AccountController(IMediator mediator, IMapper mapper) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        public async Task<ResponseViewModel> GetUserProfile([FromQuery] GetUserProfileRequest request)
        {
            var result = await _mediator.Send(new GetUserProfileQuery(request.UserId));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            var response = _mapper.Map<AccountResponseViewModel>(result.Data);
            return new SuccessResponseViewModelT<AccountResponseViewModel>(response);
        }

        [HttpPost]
        public async Task<ResponseViewModel> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
        {
            var result = await _mediator.Send(new UpdateUserProfileCommand(request.UserId, request.FullName));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Your profile has been updated successfully.");
        }

        [HttpPost]
        public async Task<ResponseViewModel> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var result = await _mediator.Send(new ChangePasswordCommand(request.UserId,request.CurrentPassword,request.NewPassword));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Your password has been changed successfully.");
        }

    }
}
