using EventHub.MVC.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "Unit")]
public class HomeControllerTests
{
    [Theory]
    [InlineData("Admin,Organizer,Attendee", "Dashboard", "Admin")]
    [InlineData("Organizer,Attendee", "Dashboard", "Organizer")]
    [InlineData("Attendee", "My", "Registrations")]
    [InlineData("", "Index", null)]
    public void AuthenticatedLanding_PrioritizesManagementRoles(string roles, string action, string? controllerName)
    {
        // Arrange
        var controller = ManagementControllerTests.Prepare(new HomeController());
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            roles.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(role => new Claim(ClaimTypes.Role, role)), "test"));

        // Act
        var result = Assert.IsType<RedirectToActionResult>(controller.Authenticated());

        // Assert
        Assert.Equal(action, result.ActionName); Assert.Equal(controllerName, result.ControllerName);
    }
}
