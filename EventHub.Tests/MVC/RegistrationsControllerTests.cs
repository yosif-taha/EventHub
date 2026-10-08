using EventHub.MVC.Controllers;
using EventHub.MVC.Models.Registrations;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Registrations;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class RegistrationsControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("https://payments.example.test/order")]
    public async Task Create_RoutesFreeRegistrationToHistoryAndPaidRegistrationToProvider(string? paymentUrl)
    {
        // Arrange
        var api = new Mock<IRegistrationApiClient>(MockBehavior.Strict);
        api.Setup(a => a.RegisterAsync(WorkflowFixture.OtherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<RegistrationResultDto>.Success(new() { RegistrationId = WorkflowFixture.AttendeeId, PaymentUrl = paymentUrl }));
        var controller = Controller(api);

        // Act
        var result = await controller.Create(WorkflowFixture.OtherId, default);

        // Assert
        if (paymentUrl is null)
        {
            Assert.Equal("My", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Contains("confirmed", controller.TempData["Success"]!.ToString());
        }
        else Assert.Equal(paymentUrl, Assert.IsType<RedirectResult>(result).Url);
        api.VerifyAll();
    }

    [Fact]
    public async Task CreateFailure_ReturnsToEventWithErrorMessage()
    {
        // Arrange
        var api = new Mock<IRegistrationApiClient>();
        api.Setup(a => a.RegisterAsync(WorkflowFixture.OtherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<RegistrationResultDto>.Failure("Event is full", ApiFailureKind.Validation));
        var controller = Controller(api);

        // Act
        var result = Assert.IsType<RedirectToActionResult>(await controller.Create(WorkflowFixture.OtherId, default));

        // Assert
        Assert.Equal("Details", result.ActionName);
        Assert.Equal("Events", result.ControllerName);
        Assert.Equal(WorkflowFixture.OtherId, result.RouteValues!["id"]);
        Assert.Equal("Event is full", controller.TempData["Error"]);
    }

    [Fact]
    public async Task PaymentReturn_IgnoresQueryStringAndDisplaysAuthoritativeApiStatus()
    {
        // Arrange
        var api = new Mock<IRegistrationApiClient>();
        var status = new RegistrationPaymentStatusDto();
        api.Setup(a => a.GetStatusAsync(WorkflowFixture.AttendeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<RegistrationPaymentStatusDto>.Success(status));
        var controller = Controller(api);
        controller.HttpContext.Request.QueryString = new QueryString("?success=true&pending=false");

        // Act
        var result = await controller.PaymentReturn(WorkflowFixture.AttendeeId, default);

        // Assert
        Assert.Same(status, Assert.IsType<ViewResult>(result).Model);
        api.Verify(a => a.GetStatusAsync(WorkflowFixture.AttendeeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ApiFailureKind.NotFound, true)]
    [InlineData(ApiFailureKind.Unavailable, false)]
    public async Task PaymentReturn_HandlesMissingAndUnavailableStatus(ApiFailureKind failure, bool notFound)
    {
        // Arrange
        var api = new Mock<IRegistrationApiClient>();
        api.Setup(a => a.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<RegistrationPaymentStatusDto>.Failure("Failure", failure));
        var controller = Controller(api);

        // Act
        var result = await controller.PaymentReturn(WorkflowFixture.AttendeeId, default);

        // Assert
        if (notFound) Assert.IsType<NotFoundResult>(result);
        else Assert.Equal("~/Views/Home/Error.cshtml", Assert.IsType<ViewResult>(result).ViewName);
    }

    private static RegistrationsController Controller(Mock<IRegistrationApiClient> api)
    {
        var context = new DefaultHttpContext();
        return new RegistrationsController(api.Object) {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
    }
}
