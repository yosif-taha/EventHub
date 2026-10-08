using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Category.Create_Category;
using EventHub.Application.Features.Category.Update_Category;
using EventHub.Application.Features.Category.Delete_Category;
using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class CategoryWorkflowTests
{
    [Fact]
    public async Task CreateUpdateDelete_PersistsChangesAndHidesDeletedCategory()
    {
        // Arrange
        using var fixture = new WorkflowFixture();

        // Act
        var created = await fixture.Mediator.Send(new CreateCategoryCommand("Technology"));
        var updated = await fixture.Mediator.Send(new UpdateCategoryCommand(created.Data, "Science"));
        var stored = await fixture.Db.EventCategories.SingleAsync();
        var deleted = await fixture.Mediator.Send(new DeleteCategoryCommand(created.Data));

        // Assert
        Assert.True(created.IsSuccess);
        Assert.True(updated.IsSuccess);
        Assert.Equal("Science", stored.Name);
        Assert.True(deleted.IsSuccess);
        Assert.Empty(await fixture.Db.EventCategories.ToListAsync());
        Assert.True((await fixture.Db.EventCategories.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task DuplicateNames_AreRejectedWithoutChangingExistingData()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var first = await fixture.Mediator.Send(new CreateCategoryCommand("Science"));
        var second = await fixture.Mediator.Send(new CreateCategoryCommand("Technology"));

        // Act
        var duplicate = await fixture.Mediator.Send(new CreateCategoryCommand("Science"));
        var renamed = await fixture.Mediator.Send(new UpdateCategoryCommand(second.Data, "Science"));

        // Assert
        Assert.Equal(ErrorCode.CategoryAlreadyExist, duplicate.ErrorCode);
        Assert.Equal(ErrorCode.CategoryAlreadyExist, renamed.ErrorCode);
        Assert.Equal(2, await fixture.Db.EventCategories.CountAsync());
        Assert.Equal("Technology", (await fixture.Db.EventCategories.SingleAsync(c => c.Id == second.Data)).Name);
    }

    [Fact]
    public async Task Delete_InUseCategoryIsRejected()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();

        // Act
        var result = await fixture.Mediator.Send(new DeleteCategoryCommand(entity.CategoryId!.Value));

        // Assert
        Assert.Equal(ErrorCode.CategoryInUse, result.ErrorCode);
        Assert.Equal(1, await fixture.Db.EventCategories.CountAsync());
    }

    [Fact]
    public async Task MissingCategory_UpdateAndDeleteReturnNotFound()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var id = WorkflowFixture.OtherId;

        // Act
        var update = await fixture.Mediator.Send(new UpdateCategoryCommand(id, "Science"));
        var delete = await fixture.Mediator.Send(new DeleteCategoryCommand(id));

        // Assert
        Assert.Equal(ErrorCode.CategoryNotFound, update.ErrorCode);
        Assert.Equal(ErrorCode.CategoryNotFound, delete.ErrorCode);
    }
}
