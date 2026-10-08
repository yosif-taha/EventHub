using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Persistence;

[Trait("Category", "Persistence")]
public sealed class ModelConstraintTests
{
    [Theory]
    [InlineData("category")]
    [InlineData("registration")]
    [InlineData("notification")]
    [InlineData("order")]
    [InlineData("transaction")]
    public async Task UniqueKeys_RejectDuplicates(string kind)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        var registration = await fixture.SeedRegistrationAsync(entity);
        switch (kind)
        {
            case "category":
                fixture.Db.Add(new EventCategory { Name = entity.Category.Name });
                break;
            case "registration":
                fixture.Db.Add(new Registration { EventId = entity.Id, UserId = registration.UserId });
                break;
            case "notification":
                fixture.Db.AddRange(
                    new Notification { EventId = entity.Id, UserId = registration.UserId, DeduplicationKey = "same" },
                    new Notification { EventId = entity.Id, UserId = registration.UserId, DeduplicationKey = "same" });
                break;
            default:
                fixture.Db.AddRange(
                    new PaymentTransaction { RegistrationId = registration.Id, MerchantOrderId = "a",
                        PaymobOrderId = kind == "order" ? "same" : null, PaymobTransactionId = kind == "transaction" ? "same" : null },
                    new PaymentTransaction { RegistrationId = registration.Id, MerchantOrderId = "b",
                        PaymobOrderId = kind == "order" ? "same" : null, PaymobTransactionId = kind == "transaction" ? "same" : null });
                break;
        }

        // Act / Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task ForeignKeys_RejectOrphanRegistration()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        fixture.Db.Add(new Registration { EventId = WorkflowFixture.OwnerId, UserId = WorkflowFixture.AttendeeId });

        // Act / Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrencyToken_RejectsStaleEventWrite()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        using var first = fixture.CreateContext();
        using var second = fixture.CreateContext();
        var a = await first.Events.AsTracking().SingleAsync();
        var b = await second.Events.AsTracking().SingleAsync();
        a.CurrentAttendeesCount = 1;
        await first.SaveChangesAsync();
        b.CurrentAttendeesCount = 2;

        // Act / Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        using var verification = fixture.CreateContext();
        Assert.Equal(1, (await verification.Events.SingleAsync()).CurrentAttendeesCount);
    }

    [Fact]
    public async Task DeletingCategory_SetsEventCategoryToNull()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        await fixture.SeedEventAsync();
        var category = await fixture.Db.EventCategories.AsTracking().SingleAsync();

        // Act
        fixture.Db.Remove(category);
        await fixture.Db.SaveChangesAsync();

        // Assert
        Assert.Null((await fixture.Db.Events.SingleAsync()).CategoryId);
    }
}
