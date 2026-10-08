using EventHub.Application.Common.Responses;
using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Persistence;

[Trait("Category", "Persistence")]
public sealed class UnitOfWorkTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_CommitsOnlySuccessfulResult(bool success)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var category = new EventCategory { Name = "Before" };
        fixture.Db.Add(category);
        await fixture.Db.SaveChangesAsync();

        // Act
        var result = await fixture.UnitOfWork.ExecuteAsync(() =>
        {
            category.Name = "After";
            return Task.FromResult(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));
        }, default);

        // Assert
        using var verification = fixture.CreateContext();
        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(success ? "After" : "Before", (await verification.EventCategories.SingleAsync()).Name);
        if (!success) Assert.Empty(fixture.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ExecuteAsync_RollsBackExceptionAndCanBeReused()
    {
        // Arrange
        using var fixture = new WorkflowFixture();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UnitOfWork.ExecuteAsync<int>(async () =>
        {
            fixture.Db.Add(new EventCategory { Name = "Rolled back" });
            await fixture.Db.SaveChangesAsync();
            throw new InvalidOperationException("failure");
        }, default));
        await fixture.UnitOfWork.ExecuteAsync(async () =>
        {
            await fixture.Db.AddAsync(new EventCategory { Name = "Committed" });
            return 42;
        }, default);

        // Assert
        using var verification = fixture.CreateContext();
        Assert.Equal("Committed", (await verification.EventCategories.SingleAsync()).Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NestedExecution_UsesOuterTransaction(bool outerSuccess)
    {
        // Arrange
        using var fixture = new WorkflowFixture();

        // Act
        await fixture.UnitOfWork.ExecuteAsync(async () =>
        {
            await fixture.UnitOfWork.ExecuteAsync(async () =>
            {
                await fixture.Db.AddAsync(new EventCategory { Name = "Nested" });
                return RequestResult<bool>.Success(true);
            }, default);
            return outerSuccess ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError);
        }, default);

        // Assert
        using var verification = fixture.CreateContext();
        Assert.Equal(outerSuccess ? 1 : 0, await verification.EventCategories.CountAsync());
    }
}
