using EventHub.Application.Features.Admin.UpdateUserRole;
using EventHub.Domin.Enums;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class UpdateUserRoleCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new UpdateUserRoleCommandValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var validator = new UpdateUserRoleCommandValidator();
        var request = CreateValidRequest() with { UserId = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.UserId), error.PropertyName);
    }

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Organizer, true)]
    [InlineData(UserRole.Attendee, true)]
    [InlineData((UserRole)(-1), false)]
    [InlineData((UserRole)999, false)]
    public void Validate_ShouldAcceptOnlySupportedRoleValues(UserRole value, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateUserRoleCommandValidator();
        var request = CreateValidRequest() with { Role = value };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectedIsValid, result.IsValid);
        if (expectedIsValid)
        {
            Assert.Empty(result.Errors);
        }
        else
        {
            Assert.NotEmpty(result.Errors);
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Role), error.PropertyName));
        }
    }

    private static UpdateUserRoleCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), UserRole.Attendee);
}
