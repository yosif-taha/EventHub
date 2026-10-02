using EventHub.Application.Common.Responses;
using EventHub.Domin.Models;
using EventHub.Persistence.Reposetories;
using EventHub.Persistence.Unit_Of_Work;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Tests;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task Failed_request_result_rolls_back_tracked_mutation()
    {
        await using var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = new EventCategory { Name = "Before", CreatedAt = DateTime.UtcNow };
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();

        var trackedCategory = await context.EventCategories.AsTracking().SingleAsync();
        var unitOfWork = new UnitOfWork(context);

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            trackedCategory.Name = "After";
            return await Task.FromResult(RequestResult<bool>.Failure(ErrorCode.ValidationError));
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        await using var verificationContext = database.CreateContext();
        Assert.Equal("Before", await verificationContext.EventCategories.Select(item => item.Name).SingleAsync());
    }

    [Fact]
    public async Task Successful_request_result_commits_tracked_mutation()
    {
        await using var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = new EventCategory { Name = "Before", CreatedAt = DateTime.UtcNow };
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();

        var trackedCategory = await context.EventCategories.AsTracking().SingleAsync();
        var unitOfWork = new UnitOfWork(context);

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            trackedCategory.Name = "After";
            return await Task.FromResult(RequestResult<bool>.Success(true));
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await using var verificationContext = database.CreateContext();
        Assert.Equal("After", await verificationContext.EventCategories.Select(item => item.Name).SingleAsync());
    }
}
