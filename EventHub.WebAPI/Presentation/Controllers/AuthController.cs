using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Features.Auth.ConfirmEmail;
using EventHub.Application.Features.Auth.Login;
using EventHub.Application.Features.Auth.PasswordReset;
using EventHub.Application.Features.Auth.RefreshTokens;
using EventHub.Application.Features.Auth.Register;
using EventHub.WebAPI.Presentation.ViewModels.Auth;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController(IMediator mediator, IMapper mapper) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly IMapper _mapper = mapper;

        [HttpPost]
        public async Task<ResponseViewModel> Login([FromBody] LoginRequestViewModel request)
        {
            var result = await _mediator.Send(new LoginQuery(request.Email, request.Password));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var response = _mapper.Map<AuthResponseViewModel>(result.Data);
            return new SuccessResponseViewModelT<AuthResponseViewModel>(response);
        }

        [HttpPost]
        public async Task<ResponseViewModel> Register([FromBody] RegisterRequestViewModel request)
        {
           var result = await  _mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName, request.Role));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            return new SuccessResponseViewModelT<Guid>(result.Data);
        }

        [HttpPost]
        public async Task<ResponseViewModel> RefreshToken([FromBody] RefreshTokenRequest request)
        {
           var result = await _mediator.Send(new RefreshTokenCommand(request.Token,request.RefreshToken));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            var response = _mapper.Map<AuthResponseViewModel>(result.Data);
            return new SuccessResponseViewModelT<AuthResponseViewModel>(response);
        }

        [HttpPost]
        public async Task<ResponseViewModel> ConfirmEmail(ConfirmEmailRequest request)
        {
           var result = await _mediator.Send(new ConfirmEmailCommand(request.UserId, request.Code));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
           return new SuccessResponseViewModel("Email Confirmed");
        }

        [HttpPost]
        public async Task<ResponseViewModel> ResendConfirmationEmail(ResendConfirmationEmailRequest request)
        {
            var result = await _mediator.Send(new ResendConfirmationEmailCommand(request.Email));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Resend Email Successfuly");
        }

        [HttpPost]
        public async Task<ResponseViewModel> ResetPassword(ResetPasswordRequest request)
        {
            var result = await _mediator.Send(new ResetPasswordCommand(request.Email,request.Code,request.NewPassword));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Your password has been reset successfully.");
        }

        [HttpPost]
        public async Task<ResponseViewModel> SendResetPassword(SendResetPasswordRequest request)
        {
            var result = await _mediator.Send(new SendResetPasswordCommand(request.Email));
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Check your inbox for a password reset link.");
        }

    }
}
