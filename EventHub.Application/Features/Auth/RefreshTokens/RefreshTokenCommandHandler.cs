using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.RefreshTokens
{
    public class RefreshTokenCommandHandler(IAuthService _authService) : IRequestHandler<RefreshTokenCommand, RequestResult<AuthResponse>>
    {
        public async Task<RequestResult<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _authService.GenerateNewTokensAsync(request.Token, request.RefreshToken, cancellationToken);
            if (!result.IsSuccess)
                return RequestResult<AuthResponse>.Failure(result.ErrorCode);
            return RequestResult<AuthResponse>.Success(result.Data!);
        }
    }
}
