using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.PasswordReset
{
    public class ResetPasswordCommandHandler(IAuthService authService) : IRequestHandler<ResetPasswordCommand, RequestResult<bool>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<RequestResult<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
           var date = await _authService.ResetPasswordAsync(request.Email,request.Code,request.NewPassword,cancellationToken);
            if(!date.IsSuccess)
                return RequestResult<bool>.Failure(date.ErrorCode);
            return RequestResult<bool>.Success(date.Data);
        }
    }
}
