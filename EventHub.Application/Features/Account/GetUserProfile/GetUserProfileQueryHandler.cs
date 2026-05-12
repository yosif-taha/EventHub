using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Account.GetUserProfile
{
    public class GetUserProfileQueryHandler(IAccountService _accountService) : IRequestHandler<GetUserProfileQuery, RequestResult<UserProfileResponse>>
    {
        public async Task<RequestResult<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var data = await  _accountService.GetUserProfileAsync(request.UserId, cancellationToken);
            if (!data.IsSuccess)
                return RequestResult<UserProfileResponse>.Failure(data.ErrorCode);
            return RequestResult<UserProfileResponse>.Success(data.Data!);
        }
    }
}
