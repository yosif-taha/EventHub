using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.Register
{
    public class RegisterCommandHandler(IAuthService authService) : IRequestHandler<RegisterCommand, RequestResult<Guid>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<RequestResult<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request.Email, request.Password, request.FullName, request.Role, cancellationToken);
            if (!result.IsSuccess)
              return RequestResult<Guid>.Failure(result.ErrorCode);
            return RequestResult<Guid>.Success(result.Data);
        }
    }
}
