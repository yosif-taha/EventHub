using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Account.ChangePassword
{
    public class ChangePasswordCommandHandler(IAccountService _accountService) : IRequestHandler<ChangePasswordCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var data = await _accountService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
