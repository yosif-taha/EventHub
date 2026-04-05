using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.ChangePassword
{
    public class ChangePasswordCommandHandler(IAccountService accountService) : IRequestHandler<ChangePasswordCommand, RequestResult<bool>>
    {
        private readonly IAccountService _accountService = accountService;

        public async Task<RequestResult<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
          var data = await _accountService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
