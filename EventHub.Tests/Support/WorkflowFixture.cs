using AutoMapper;
using EventHub.Application;
using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Common;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Infrastructure.Common;
using EventHub.Persistence.Data.Contexts;
using EventHub.Persistence.Reposetories;
using EventHub.Persistence.Unit_Of_Work;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventHub.Tests.Support;

// Each fixture owns its connection, context, service scope, and external-service doubles.
internal sealed class WorkflowFixture : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    public static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AttendeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid OtherId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly DateTime Future = new(2090, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public TestUserContext User { get; } = new();
    public Mock<IAccountService> Accounts { get; } = new();
    public Mock<IEmailService> Email { get; } = new();
    public Mock<IPaymobService> Payments { get; } = new();
    public Mock<IUserManagementService> Users { get; } = new();
    public TestEventDbContext Db => (TestEventDbContext)scope.ServiceProvider.GetRequiredService<EventDbContext>();
    public IMediator Mediator => scope.ServiceProvider.GetRequiredService<IMediator>();
    public IMapper Mapper => scope.ServiceProvider.GetRequiredService<IMapper>();
    public IUnitOfWork UnitOfWork => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    public T Service<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    public GenericRepository<T> Repository<T>() where T : BaseModel => new(Db);

    public WorkflowFixture()
    {
        connection.Open();
        Accounts.Setup(x => x.GetUserProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<UserProfileResponse>.Success(new("attendee@example.test", "attendee", "Test Attendee", "+201000000000")));
        Email.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Payments.Setup(x => x.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymobPaymentResponse("https://payments.example.test/order", "100"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddScoped<EventDbContext>(_ => CreateContext());
        services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(
            new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EventDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDbExecutor, DbExecutor>();
        services.AddSingleton<IUserContext>(User);
        services.AddSingleton(Accounts.Object);
        services.AddSingleton(Email.Object);
        services.AddSingleton(Payments.Object);
        services.AddSingleton(Users.Object);
        provider = services.BuildServiceProvider();
        scope = provider.CreateScope();
        Db.Database.EnsureCreated();
    }

    public TestEventDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TestEventDbContext>().UseSqlite(connection).Options);

    public async Task<Event> SeedEventAsync(decimal price = 0, int count = 0, int capacity = 10)
    {
        if (!await Db.Users.AnyAsync())
        {
            Db.Users.AddRange(
                new ApplicationUser { Id = OwnerId, Email = "owner@example.test", UserName = "owner", FullName = "Owner" },
                new ApplicationUser { Id = AttendeeId, Email = "attendee@example.test", UserName = "attendee", FullName = "Attendee", EmailConfirmed = true },
                new ApplicationUser { Id = OtherId, Email = "other@example.test", UserName = "other", FullName = "Other" });
        }
        var category = new EventCategory { Name = "Category " + (await Db.EventCategories.CountAsync() + 1) };
        var entity = new Event
        {
            Title = "Community event", Description = "Description", Location = "Cairo",
            OrganizerId = OwnerId, Category = category, EventDate = Future,
            Status = EventStatus.Scheduled, Price = price, CurrentAttendeesCount = count, MaxAttendees = capacity
        };
        Db.Events.Add(entity);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return entity;
    }

    public async Task<Registration> SeedRegistrationAsync(Event entity, RegistrationStatus status = RegistrationStatus.Confirmed, Guid? userId = null)
    {
        var registration = new Registration
        {
            EventId = entity.Id, UserId = userId ?? AttendeeId, Status = status,
            RegistrationDate = new DateTime(2026, 1, 1), CreatedAt = new DateTime(2026, 1, 1)
        };
        Db.Registrations.Add(registration);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return registration;
    }

    public async Task<PaymentTransaction> SeedPaymentAsync(Registration registration,
        PaymentTransactionStatus status = PaymentTransactionStatus.Pending,
        PaymentOrderCreationStatus orderStatus = PaymentOrderCreationStatus.Pending)
    {
        var payment = new PaymentTransaction
        {
            RegistrationId = registration.Id, Amount = 100m, Currency = "EGP", Status = status,
            MerchantOrderId = registration.Id.ToString(), OrderCreationStatus = orderStatus,
            CreatedAt = new DateTime(2026, 1, 1)
        };
        Db.PaymentTransactions.Add(payment);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return payment;
    }

    public void Dispose()
    {
        scope.Dispose();
        provider.Dispose();
        connection.Dispose();
    }
}

internal sealed class TestUserContext : IUserContext
{
    public Guid UserId { get; set; } = WorkflowFixture.AttendeeId;
    public string? Email => "attendee@example.test";
    public bool IsAuthenticated { get; set; } = true;
    public string Role { get; set; } = RoleNames.Attendee;
    public bool IsInRole(string role) => IsAuthenticated && Role == role;
}

internal sealed class TestEventDbContext(DbContextOptions<TestEventDbContext> options) : EventDbContext(options)
{
    private int nextRefreshTokenId;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // SQLite cannot generate an integer identity inside a composite primary key.
        builder.Entity<ApplicationUser>().OwnsMany(u => u.RefreshTokens)
            .Property<int>("Id").ValueGeneratedNever();
        // SQLite has no SQL Server rowversion generator. Keep concurrency predicates, but
        // generate deterministic per-context versions on writes. This is not a SQL Server test.
        foreach (var type in builder.Model.GetEntityTypes())
        {
            var property = type.FindProperty("RowVersion");
            if (property is null) continue;
            property.ValueGenerated = ValueGenerated.Never;
            property.IsConcurrencyToken = true;
            property.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
            property.SetAfterSaveBehavior(PropertySaveBehavior.Save);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<RefreshTokens>().Where(e => e.State == EntityState.Added))
            if (entry.Property<int>("Id").CurrentValue == 0)
                entry.Property<int>("Id").CurrentValue = ++nextRefreshTokenId;
        foreach (var entry in ChangeTracker.Entries().Where(e =>
                     e.State is EntityState.Added or EntityState.Modified && e.Metadata.FindProperty("RowVersion") != null))
        {
            var property = entry.Property("RowVersion");
            var old = property.OriginalValue as byte[];
            var version = old is { Length: 8 } ? BitConverter.ToInt64(old) : 0;
            property.CurrentValue = BitConverter.GetBytes(version + 1);
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
