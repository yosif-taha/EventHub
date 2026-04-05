using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.GetUserProfile
{
    public class GetUserProfileQueryHandler(IAccountService accountService) : IRequestHandler<GetUserProfileQuery, RequestResult<UserProfileResponse>>
    {
        private readonly IAccountService _accountService = accountService;

        public async Task<RequestResult<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
          var data = await  _accountService.GetUserProfileAsync(request.UserId, cancellationToken);
            if (!data.IsSuccess)
                return RequestResult<UserProfileResponse>.Failure(data.ErrorCode);
            return RequestResult<UserProfileResponse>.Success(data.Data!);
        }
    }
}
