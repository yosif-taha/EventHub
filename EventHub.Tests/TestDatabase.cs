using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Persistence.Data.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EventHub.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<TestEventDbContext> _options;

    public TestDatabase()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<TestEventDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TestEventDbContext CreateContext() => new(_options);

    public async Task<(ApplicationUser Attendee, Event Event, Registration Registration, PaymentTransaction Transaction)> SeedPendingPaidRegistrationAsync()
    {
        await using var context = CreateContext();
        var attendee = new ApplicationUser
        {
            Email = "attendee@example.test",
            UserName = "attendee@example.test",
            FullName = "Test Attendee",
            EmailConfirmed = true
        };
        var @event = new Event
        {
            Title = "Paid event",
            Description = "Test event",
            Location = "Cairo",
            EventDate = DateTime.UtcNow.AddDays(2),
            MaxAttendees = 10,
            CurrentAttendeesCount = 1,
            Price = 100m,
            Status = EventStatus.Scheduled,
            OrganizerId = attendee.Id,
            CreatedAt = DateTime.UtcNow,
            RowVersion = [0]
        };
        var registration = new Registration
        {
            EventId = @event.Id,
            UserId = attendee.Id,
            Status = RegistrationStatus.Pending,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        var transaction = new PaymentTransaction
        {
            RegistrationId = registration.Id,
            Amount = 100m,
            Currency = "EGP",
            Status = PaymentTransactionStatus.Pending,
            MerchantOrderId = $"{registration.Id:N}-{Guid.NewGuid():N}",
            OrderCreationStatus = PaymentOrderCreationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RowVersion = [0]
        };

        context.AddRange(attendee, @event, registration, transaction);
        await context.SaveChangesAsync();
        return (attendee, @event, registration, transaction);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

internal sealed class TestEventDbContext(DbContextOptions<TestEventDbContext> options) : EventDbContext(options)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetRowVersionsForAddedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetRowVersionsForAddedEntities()
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(item => item.State == EntityState.Added && item.Metadata.FindProperty("RowVersion") is not null))
        {
            var property = entry.Property("RowVersion");
            property.CurrentValue = new byte[] { 0 };
            property.IsTemporary = false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite does not provide SQL Server's rowversion generation. Keep concurrency tokens in
        // the production model while making this relational test model able to insert fixtures.
        ConfigureRowVersion(modelBuilder.Entity<Event>().Property(item => item.RowVersion));
        ConfigureRowVersion(modelBuilder.Entity<PaymentTransaction>().Property(item => item.RowVersion));
        ConfigureRowVersion(modelBuilder.Entity<Notification>().Property(item => item.RowVersion));
    }

    private static void ConfigureRowVersion(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<byte[]> property)
    {
        property.IsConcurrencyToken(false).ValueGeneratedNever().HasDefaultValue(new byte[] { 0 });
        property.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
    }
}
