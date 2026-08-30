using System.Security.Claims;

namespace EventHub.MVC.Services.Auth;

public interface IMvcAuthenticationService
{
    ClaimsPrincipal? CreatePrincipal(AuthResponseDto response);
}
