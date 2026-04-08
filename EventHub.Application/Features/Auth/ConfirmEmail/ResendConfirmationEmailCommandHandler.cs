using AutoMapper.Configuration.Annotations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.ConfirmEmail
{
    public class ResendConfirmationEmailCommandHandler(IAuthService _authService) : IRequestHandler<ResendConfirmationEmailCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = await _authService.ResendConfirmationEmailAsync(request.Email, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
