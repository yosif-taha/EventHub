using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.PasswordReset
{
    public class SendResetPasswordCommandHandler(IAuthService _authService) : IRequestHandler<SendResetPasswordCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(SendResetPasswordCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = await _authService.SendResetPasswordAsync(request.Email, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
