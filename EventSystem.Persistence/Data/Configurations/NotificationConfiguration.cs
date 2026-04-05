using EventHub.Domin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Persistence.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).ValueGeneratedNever();

            builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();

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
