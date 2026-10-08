using EventHub.Application.Contracts;
using EventHub.Application.Common.Responses;
using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Features.Auth.ConfirmEmail;
using EventHub.Application.Features.Auth.PasswordReset;
using EventHub.Application.Features.Auth.Login;
using EventHub.Application.Features.Auth.Register;
using EventHub.Application.Features.Auth.RefreshTokens;
using EventHub.Tests.Support;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "MockUnit")]
public class AuthHandlerTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfirmEmail_ForwardsArgumentsCancellationAndServiceOutcome(bool success)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        service.Setup(s => s.ConfirmEmailAsync("user", "code", cancellation.Token)).ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.InvalidCredentials));
        var handler = new ConfirmEmailCommandHandler(service.Object);

        // Act
        var result = await handler.Handle(new ConfirmEmailCommand("user", "code"), cancellation.Token);

        // Assert
        Assert.Equal(success, result.IsSuccess);
        if (success) Assert.True(result.Data);
        else Assert.Equal(ErrorCode.InvalidCredentials, result.ErrorCode);
        service.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResendConfirmation_ForwardsArgumentsCancellationAndServiceOutcome(bool success)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        service.Setup(s => s.ResendConfirmationEmailAsync("user@example.test", cancellation.Token)).ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.InvalidCredentials));
        var handler = new ResendConfirmationEmailCommandHandler(service.Object);

        // Act
        var result = await handler.Handle(new ResendConfirmationEmailCommand("user@example.test"), cancellation.Token);

        // Assert
        Assert.Equal(success, result.IsSuccess);
        if (success) Assert.True(result.Data);
        else Assert.Equal(ErrorCode.InvalidCredentials, result.ErrorCode);
        service.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendResetPassword_ForwardsArgumentsCancellationAndServiceOutcome(bool success)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        service.Setup(s => s.SendResetPasswordAsync("user@example.test", cancellation.Token)).ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.InvalidCredentials));
        var handler = new SendResetPasswordCommandHandler(service.Object);

        // Act
        var result = await handler.Handle(new SendResetPasswordCommand("user@example.test"), cancellation.Token);

        // Assert
        Assert.Equal(success, result.IsSuccess);
        if (success) Assert.True(result.Data);
        else Assert.Equal(ErrorCode.InvalidCredentials, result.ErrorCode);
        service.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResetPassword_ForwardsArgumentsCancellationAndServiceOutcome(bool success)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        service.Setup(s => s.ResetPasswordAsync("user@example.test", "code", "Changed!Pass123", cancellation.Token)).ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.InvalidCredentials));
        var handler = new ResetPasswordCommandHandler(service.Object);

        // Act
        var result = await handler.Handle(new ResetPasswordCommand("user@example.test", "code", "Changed!Pass123"), cancellation.Token);

        // Assert
        Assert.Equal(success, result.IsSuccess);
        if (success) Assert.True(result.Data);
        else Assert.Equal(ErrorCode.InvalidCredentials, result.ErrorCode);
        service.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoginAndRefresh_PreserveAuthPayloadOrError(bool success)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        var data = new AuthResponse("id", "user@example.test", "User", "jwt", 60, "refresh", WorkflowFixture.Future);
        var response = success ? RequestResult<AuthResponse?>.Success(data) : RequestResult<AuthResponse?>.Failure(ErrorCode.InvalidCredentials);
        service.Setup(s => s.LoginAsync("user@example.test", "password", cancellation.Token)).ReturnsAsync(response);
        service.Setup(s => s.GenerateNewTokensAsync("jwt", "refresh", cancellation.Token)).ReturnsAsync(response);

        // Act
        var login = await new LoginQueryHandler(service.Object).Handle(new LoginQuery("user@example.test", "password"), cancellation.Token);
        var refresh = await new RefreshTokenCommandHandler(service.Object).Handle(new RefreshTokenCommand("jwt", "refresh"), cancellation.Token);

        // Assert
        Assert.Equal(success, login.IsSuccess);
        Assert.Equal(success, refresh.IsSuccess);
        if (success) { Assert.Same(data, login.Data); Assert.Same(data, refresh.Data); }
        else { Assert.Equal(ErrorCode.InvalidCredentials, login.ErrorCode); Assert.Equal(ErrorCode.InvalidCredentials, refresh.ErrorCode); }
        service.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Register_ForwardsProfileAndPropagatesResult(bool success)
    {
        // Arrange
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        service.Setup(s => s.RegisterAsync("user@example.test", "password", "Test User", "+201000000000", default))
            .ReturnsAsync(success ? RequestResult<Guid>.Success(WorkflowFixture.AttendeeId) : RequestResult<Guid>.Failure(ErrorCode.UserAlreadyExist));

        // Act
        var result = await new RegisterCommandHandler(service.Object).Handle(new RegisterCommand("user@example.test", "password", "Test User", "+201000000000"), default);

        // Assert
        Assert.Equal(success, result.IsSuccess);
        if (success) Assert.Equal(WorkflowFixture.AttendeeId, result.Data);
        else Assert.Equal(ErrorCode.UserAlreadyExist, result.ErrorCode);
        service.VerifyAll();
    }
}
