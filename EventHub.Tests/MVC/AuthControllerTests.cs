using EventHub.MVC.Controllers;
using EventHub.MVC.Models.Auth;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class AuthControllerTests
{
    [Theory]
    [InlineData("/Events", true, "/Events")]
    [InlineData("https://external.example.test/", false, "/")]
    [InlineData(null, false, "/")]
    public async Task Login_CreatesSessionWithTokenAndOnlyRedirectsLocally(string? returnUrl, bool local, string expected)
    {
        // Arrange
        var api = new Mock<IAuthApiClient>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "User")], "test"));
        var identity = new Mock<IMvcAuthenticationService>();
        var response = new AuthResponseDto { Id = "id", Token = "access-token", ExpiresIn = 600 };
        api.Setup(a => a.LoginAsync(It.IsAny<LoginViewModel>(), default)).ReturnsAsync(ApiCallResult<AuthResponseDto>.Success(response));
        identity.Setup(s => s.CreatePrincipal(response)).Returns(principal);
        var authentication = new Mock<IAuthenticationService>();
        AuthenticationProperties? properties = null;
        authentication.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, principal, It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?>((_, _, _, p) => properties = p).Returns(Task.CompletedTask);
        using var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var controller = ManagementControllerTests.Prepare(new AuthController(api.Object, identity.Object));
        controller.HttpContext.RequestServices = services;
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(returnUrl)).Returns(local);
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/");
        controller.Url = url.Object;

        // Act
        var result = await controller.Login(new LoginViewModel { Email = "user@example.test", Password = "password", ReturnUrl = returnUrl }, default);

        // Assert
        Assert.Equal(expected, Assert.IsType<LocalRedirectResult>(result).Url);
        Assert.NotNull(properties);
        Assert.False(properties.IsPersistent);
        Assert.False(properties.AllowRefresh);
        Assert.Equal("access-token", properties.GetTokenValue("access_token"));
        Assert.NotNull(properties.ExpiresUtc);
        authentication.VerifyAll();
    }

    [Theory]
    [InlineData("model")]
    [InlineData("backend")]
    [InlineData("principal")]
    public async Task LoginFailure_RedisplaysInputWithoutSigningIn(string stage)
    {
        // Arrange
        var api = new Mock<IAuthApiClient>();
        var identity = new Mock<IMvcAuthenticationService>();
        api.Setup(a => a.LoginAsync(It.IsAny<LoginViewModel>(), default))
            .ReturnsAsync(stage == "backend" ? ApiCallResult<AuthResponseDto>.Failure("Invalid credentials", ApiFailureKind.Unauthorized) : ApiCallResult<AuthResponseDto>.Success(new()));
        var controller = ManagementControllerTests.Prepare(new AuthController(api.Object, identity.Object));
        if (stage == "model") controller.ModelState.AddModelError("Email", "Required");
        var model = new LoginViewModel();

        // Act
        var result = await controller.Login(model, default);

        // Assert
        Assert.Same(model, Assert.IsType<ViewResult>(result).Model);
        Assert.False(controller.ModelState.IsValid);
        if (stage == "model") api.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Register_HandlesSuccessAndBackendRejection(bool success)
    {
        // Arrange
        var api = new Mock<IAuthApiClient>();
        api.Setup(a => a.RegisterAsync(It.IsAny<RegisterViewModel>(), default)).ReturnsAsync(success
            ? ApiCallResult<Guid>.Success(Guid.Empty) : ApiCallResult<Guid>.Failure("Email is already registered", ApiFailureKind.Validation));
        var controller = ManagementControllerTests.Prepare(new AuthController(api.Object, Mock.Of<IMvcAuthenticationService>()));
        var model = new RegisterViewModel();

        // Act
        var result = await controller.Register(model, default);

        // Assert
        if (success) Assert.Equal("RegistrationComplete", Assert.IsType<RedirectToActionResult>(result).ActionName);
        else { Assert.Same(model, Assert.IsType<ViewResult>(result).Model); Assert.False(controller.ModelState.IsValid); }
    }

    [Fact]
    public void MissingConfirmationOrResetParameters_ReturnBadRequest()
    {
        // Arrange
        var controller = new AuthController(Mock.Of<IAuthApiClient>(), Mock.Of<IMvcAuthenticationService>());

        // Act / Assert
        Assert.IsType<BadRequestResult>(controller.ConfirmEmail(null, "code"));
        Assert.IsType<BadRequestResult>(controller.ConfirmEmail(Guid.Empty, ""));
        Assert.IsType<BadRequestResult>(controller.ResetPassword(null, "code"));
        Assert.IsType<BadRequestResult>(controller.ResetPassword("user@example.test", ""));
    }
}
