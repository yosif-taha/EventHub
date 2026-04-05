using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler(IAccountService accountService) : IRequestHandler<UpdateUserProfileCommand, RequestResult<bool>>
    {
        private readonly IAccountService _accountService = accountService;

        public async Task<RequestResult<bool>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
           var data = await _accountService.UpdateUserProfileAsync(request.UserId, request.FullName, cancellationToken);
             if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
             return RequestResult<bool>.Success(data.Data);
        }
    }
}
