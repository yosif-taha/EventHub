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

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController(IMediator _mediator, IMapper _mapper) : ControllerBase
    {
        [HttpPost]
        public async Task<ResponseViewModel> Login([FromBody] LoginRequestViewModel request, CancellationToken ct)
        {
            var result = await _mediator.Send(new LoginQuery(request.Email, request.Password), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var response = _mapper.Map<AuthResponseViewModel>(result.Data);
            return new SuccessResponseViewModelT<AuthResponseViewModel>(response);
        }

        [HttpPost]
        public async Task<ResponseViewModel> Register([FromBody] RegisterRequestViewModel request, CancellationToken ct)
        {
           var result = await  _mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName, request.Role), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            return new SuccessResponseViewModelT<Guid>(result.Data);
        }

        [HttpPost]
        public async Task<ResponseViewModel> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
           var result = await _mediator.Send(new RefreshTokenCommand(request.Token,request.RefreshToken), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
            var response = _mapper.Map<AuthResponseViewModel>(result.Data);
            return new SuccessResponseViewModelT<AuthResponseViewModel>(response);
        }

        [HttpPost]
        public async Task<ResponseViewModel> ConfirmEmail(ConfirmEmailRequest request, CancellationToken ct)
        {
           var result = await _mediator.Send(new ConfirmEmailCommand(request.UserId, request.Code), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode,result.ErrorCode.GetDescription());
           return new SuccessResponseViewModel("Email Confirmed");
        }

        [HttpPost]
        public async Task<ResponseViewModel> ResendConfirmationEmail(ResendConfirmationEmailRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new ResendConfirmationEmailCommand(request.Email), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Resend Email Successfuly");
        }

        [HttpPost]
        public async Task<ResponseViewModel> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new ResetPasswordCommand(request.Email,request.Code,request.NewPassword), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Your password has been reset successfully.");
        }

        [HttpPost]
        public async Task<ResponseViewModel> SendResetPassword(SendResetPasswordRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new SendResetPasswordCommand(request.Email), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel("Check your inbox for a password reset link.");
        }

    }
}
