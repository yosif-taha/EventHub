using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.PasswordReset
{
    public class SendResetPasswordCommandHandler(IAuthService authService) : IRequestHandler<SendResetPasswordCommand, RequestResult<bool>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<RequestResult<bool>> Handle(SendResetPasswordCommand request, CancellationToken cancellationToken)
        {
           var data = await _authService.SendResetPasswordAsync(request.Email, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
