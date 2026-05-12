using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandHandler(IAuthService _authService) : IRequestHandler<ConfirmEmailCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var data = await _authService.ConfirmEmailAsync(request.UserId, request.Code, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
