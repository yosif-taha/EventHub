using EventHub.Domin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Persistence.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).ValueGeneratedNever();

            builder.Property(n => n.Subject).HasMaxLength(250).IsRequired();
            builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(n => n.DeliveryStatus)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(EventHub.Domin.Enums.NotificationDeliveryStatus.Pending);
            builder.Property(n => n.DeduplicationKey).HasMaxLength(200);
            builder.Property(n => n.RowVersion).IsRowVersion();
            builder.HasIndex(n => n.DeduplicationKey)
                   .IsUnique()
                   .HasFilter("[DeduplicationKey] IS NOT NULL");
            builder.HasIndex(n => new { n.DeliveryStatus, n.NotificationDate });

            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notifications)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.Event)
                   .WithMany(e => e.Notifications)
                   .HasForeignKey(n => n.EventId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
