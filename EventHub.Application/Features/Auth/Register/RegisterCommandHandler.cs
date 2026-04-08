using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;

namespace EventHub.Application.Features.Auth.Register
{
    public class RegisterCommandHandler(IAuthService _authService) : IRequestHandler<RegisterCommand, RequestResult<Guid>>
    {
        public async Task<RequestResult<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _authService.RegisterAsync(request.Email, request.Password, request.FullName, request.Role, cancellationToken);
            if (!result.IsSuccess)
              return RequestResult<Guid>.Failure(result.ErrorCode);
            return RequestResult<Guid>.Success(result.Data);
        }
    }
}
