using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.RefreshTokens
{
    public class RefreshTokenCommandHandler(IAuthService authService) : IRequestHandler<RefreshTokenCommand, RequestResult<AuthResponse>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<RequestResult<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.GenerateNewTokensAsync(request.Token, request.RefreshToken, cancellationToken);
            if (!result.IsSuccess)
                return RequestResult<AuthResponse>.Failure(result.ErrorCode);
            return RequestResult<AuthResponse>.Success(result.Data!);
        }
    }
}
