using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.Login
{
    public class LoginQueryHandler(IAuthService authService) : IRequestHandler<LoginQuery, RequestResult<AuthResponse>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<RequestResult<AuthResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
           var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            if (!result.IsSuccess)
                return RequestResult<AuthResponse>.Failure(result.ErrorCode);
            return RequestResult<AuthResponse>.Success(result.Data!);
        }
    }
}
