using EventHub.MVC.Models.Auth;
using EventHub.MVC.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.MVC.Controllers;

public sealed class AuthController(IAuthApiClient authApiClient, IMvcAuthenticationService mvcAuthenticationService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authApiClient.LoginAsync(model, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var principal = mvcAuthenticationService.CreatePrincipal(result.Data);
        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, "We could not establish a secure session. Please try again.");
            return View(model);
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(result.Data.ExpiresIn);
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false,
            ExpiresUtc = expiresAt
        };
        properties.StoreTokens([
            new AuthenticationToken { Name = "access_token", Value = result.Data.Token },
            new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("O") }
        ]);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : Url.Action("Index", "Home")!);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authApiClient.RegisterAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        return RedirectToAction(nameof(RegistrationComplete));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult RegistrationComplete() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ConfirmEmail(Guid? userId, string? code)
    {
        if (!userId.HasValue || string.IsNullOrWhiteSpace(code))
            return BadRequest();

        return View(new ConfirmEmailViewModel { UserId = userId.Value, Code = code });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authApiClient.ConfirmEmailAsync(model.UserId, model.Code, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        return View("ConfirmEmailComplete");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            return BadRequest();

        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authApiClient.ResetPasswordAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        return View("ResetPasswordComplete");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
