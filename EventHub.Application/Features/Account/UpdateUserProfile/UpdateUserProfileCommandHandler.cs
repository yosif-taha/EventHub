using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Account.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler(IAccountService _accountService) : IRequestHandler<UpdateUserProfileCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = await _accountService.UpdateUserProfileAsync(request.UserId, request.FullName, cancellationToken);
             if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
             return RequestResult<bool>.Success(data.Data);
        }
    }
}
