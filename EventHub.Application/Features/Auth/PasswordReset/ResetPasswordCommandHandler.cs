using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.PasswordReset
{
    public class ResetPasswordCommandHandler(IAuthService _authService) : IRequestHandler<ResetPasswordCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = await _authService.ResetPasswordAsync(request.Email,request.Code,request.NewPassword,cancellationToken);
            if(!date.IsSuccess)
                return RequestResult<bool>.Failure(date.ErrorCode);
            return RequestResult<bool>.Success(date.Data);
        }
    }
}
