using EventHub.Application.Common.Dtos.Category;
using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Persistence;

[Trait("Category", "Persistence")]
public sealed class GenericRepositoryTests
{
    [Fact]
    public async Task Reads_RespectSoftDeletionAndTrackingChoice()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var active = new EventCategory { Name = "Active" };
        var deleted = new EventCategory { Name = "Deleted", IsDeleted = true };
        fixture.Db.AddRange(active, deleted);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();
        var repository = fixture.Repository<EventCategory>();

        // Act
        var result = await repository.GetByIdAsync(active.Id, default);
        var hidden = await repository.GetByIdAsync(deleted.Id, default);

        // Assert
        Assert.NotNull(result);
        Assert.Null(hidden);
        Assert.Empty(fixture.Db.ChangeTracker.Entries());
        Assert.Single(await repository.GetAll().ToListAsync());
        Assert.False(await repository.AnyAsync(c => c.Id == deleted.Id, default));
        Assert.Null(await repository.GetByIdAsync(Guid.Empty, default));
        Assert.NotNull(await repository.GetByIdAsTrackingAsync(active.Id, default));
        Assert.Single(fixture.Db.ChangeTracker.Entries());
        var projection = await repository.GetByIdProjectedAsync<CategoryDto>(c => c.Id == active.Id, fixture.Mapper.ConfigurationProvider, default);
        Assert.Equal("Active", projection!.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveInclude_UpdatesOnlySelectedFields(bool tracked)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var category = new EventCategory { Name = "Before", CreatedAt = new DateTime(2026, 1, 1) };
        fixture.Db.Add(category);
        await fixture.Db.SaveChangesAsync();
        if (!tracked) fixture.Db.ChangeTracker.Clear();

        // Act
        fixture.Repository<EventCategory>().SaveInclude(new EventCategory { Id = category.Id, Name = "After" }, nameof(EventCategory.Name));
        await fixture.Db.SaveChangesAsync();

        // Assert
        using var verification = fixture.CreateContext();
        var saved = await verification.EventCategories.SingleAsync();
        Assert.Equal("After", saved.Name);
        Assert.Equal(new DateTime(2026, 1, 1), saved.CreatedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SoftDelete_HidesRecordEvenWhenAnotherInstanceIsTracked(bool tracked)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var category = new EventCategory { Name = "Delete me" };
        fixture.Db.Add(category);
        await fixture.Db.SaveChangesAsync();
        if (!tracked) fixture.Db.ChangeTracker.Clear();

        // Act
        fixture.Repository<EventCategory>().SoftDelete(new EventCategory { Id = category.Id });
        await fixture.Db.SaveChangesAsync();

        // Assert
        using var verification = fixture.CreateContext();
        Assert.Empty(await verification.EventCategories.ToListAsync());
        Assert.True((await verification.EventCategories.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task TrackingLookup_CanReadPendingAdditionWithinUnitOfWork()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var category = new EventCategory { Name = "Not saved yet" };
        var repository = fixture.Repository<EventCategory>();
        await repository.AddAsync(category, default);

        // Act
        var found = await repository.GetByIdAsTrackingAsync(category.Id, default);

        // Assert
        Assert.Same(category, found);
    }
}
