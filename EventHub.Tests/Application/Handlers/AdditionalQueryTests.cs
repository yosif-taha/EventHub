using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Category.Get_All_Categories;
using EventHub.Application.Features.Category.Get_Category_By_Id;
using EventHub.Application.Features.Notifications.SendEventAnnouncement;
using EventHub.Domin.Constants;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class AdditionalQueryTests
{
    [Fact]
    public async Task Categories_ProjectRelatedEventCountsAndMissingCategoryReturnsNotFound()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();

        // Act
        var all = await fixture.Mediator.Send(new GetAllCategoriesQuery());
        var detail = await fixture.Mediator.Send(new GetCategoryByIdQuery(entity.CategoryId!.Value));
        var missing = await fixture.Mediator.Send(new GetCategoryByIdQuery(WorkflowFixture.OtherId));

        // Assert
        var category = Assert.Single(all.Data!);
        Assert.Equal(entity.CategoryId, category.Id);
        Assert.Equal(entity.Category.Name, category.Name);
        Assert.Equal(1, category.EventsCount);
        Assert.Equal(category, detail.Data);
        Assert.Equal(ErrorCode.CategoryNotFound, missing.ErrorCode);
    }

    [Theory]
    [InlineData(RoleNames.Admin, false, true)]
    [InlineData(RoleNames.Organizer, true, true)]
    [InlineData(RoleNames.Organizer, false, false)]
    [InlineData(RoleNames.Attendee, true, false)]
    public async Task Announcements_RequireManagementOwnershipAndQueueConfirmedAudience(string role, bool owner, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.SeedRegistrationAsync(entity);
        fixture.User.Role = role;
        fixture.User.UserId = owner ? WorkflowFixture.OwnerId : WorkflowFixture.OtherId;

        // Act
        var result = await fixture.Mediator.Send(new SendEventAnnouncementCommand(entity.Id, "Schedule update", "Please arrive early."));

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? 1 : 0, await fixture.Db.Notifications.CountAsync());
        if (allowed) Assert.Equal("Please arrive early.", (await fixture.Db.Notifications.SingleAsync()).Message);
        else Assert.Equal(ErrorCode.Forbidden, result.ErrorCode);
    }
}
