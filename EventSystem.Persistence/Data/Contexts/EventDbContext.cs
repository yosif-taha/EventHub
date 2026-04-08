using EventHub.Domin.Common;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace EventHub.Persistence.Data.Contexts
{
    public class EventDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public EventDbContext(DbContextOptions<EventDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Registration> Registrations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder.LogTo(log => Debug.WriteLine(log), LogLevel.Information).
                EnableSensitiveDataLogging(true); // Enable sensitive data logging for debugging purposes

            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Default tracking behavior set to NoTracking
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());


            modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
            {
                entity.HasKey(l => new { l.LoginProvider, l.ProviderKey });
                entity.ToTable("UserLogins");
            });

            modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
            {
                entity.HasKey(r => new { r.UserId, r.RoleId });
                entity.ToTable("UserRoles");
            });

            // Map Identity entities to custom table names
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

            // ignore this tables from mapping to tables in DB
            modelBuilder.Ignore<IdentityUserToken<Guid>>();

            CheckIsDeletedQueryFilter(modelBuilder); // Apply global query filter for IsDeleted property

        }

        private void CheckIsDeletedQueryFilter(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType)) // لو عامل Interface اسمها ISoftDelete
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var filter = Expression.Lambda(
                        Expression.Not(Expression.Property(parameter, "IsDeleted")),
                        parameter);

                    entityType.SetQueryFilter(filter);
                }
            }
        }
    }
}
