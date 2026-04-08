using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.Login
{
    public class LoginQueryHandler(IAuthService _authService) : IRequestHandler<LoginQuery, RequestResult<AuthResponse>>
    {
        public async Task<RequestResult<AuthResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            if (!result.IsSuccess)
                return RequestResult<AuthResponse>.Failure(result.ErrorCode);
            return RequestResult<AuthResponse>.Success(result.Data!);
        }
    }
}
