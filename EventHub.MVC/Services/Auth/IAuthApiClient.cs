using EventHub.MVC.Models.Auth;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Auth;

public interface IAuthApiClient
{
    Task<ApiCallResult<AuthResponseDto>> LoginAsync(LoginViewModel request, CancellationToken cancellationToken);
    Task<ApiCallResult<Guid>> RegisterAsync(RegisterViewModel request, CancellationToken cancellationToken);
    Task<ApiCallResult<bool>> ConfirmEmailAsync(Guid userId, string code, CancellationToken cancellationToken);
    Task<ApiCallResult<bool>> ResetPasswordAsync(ResetPasswordViewModel request, CancellationToken cancellationToken);
}
