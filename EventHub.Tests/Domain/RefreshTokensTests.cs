using EventHub.Domin.Models;
using Xunit;

namespace EventHub.Tests.Domain;

public sealed class RefreshTokensTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void IsActive_RequiresUnexpiredAndUnrevokedToken(bool expired, bool revoked, bool expected)
    {
        // Arrange
        var token = new RefreshTokens
        {
            ExpiresOn = expired ? DateTime.MinValue : DateTime.MaxValue,
            RevokedOn = revoked ? new DateTime(2026, 1, 1) : null
        };

        // Act
        var active = token.IsActive;

        // Assert
        Assert.Equal(expected, active);
        Assert.Equal(expired, token.IsExpired);
    }
}
