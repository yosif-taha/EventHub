using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Account.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler(IAccountService _accountService, IUserContext _userContext) : IRequestHandler<UpdateUserProfileCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var data = await _accountService.UpdateUserProfileAsync(_userContext.UserId.ToString(), request.FullName, cancellationToken);
             if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
             return RequestResult<bool>.Success(data.Data);
        }
    }
}
