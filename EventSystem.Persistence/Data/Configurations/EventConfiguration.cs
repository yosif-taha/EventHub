using EventHub.Domin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Persistence.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(2000);
            builder.Property(e => e.Location).HasMaxLength(500).IsRequired();
            builder.Property(e => e.IsAvailable).HasDefaultValue(true);
            builder.Property(e => e.Price).HasDefaultValue(0);

           
            builder.Property(e => e.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50);
   
            builder.HasOne(e => e.Category)
                   .WithMany(c => c.Events)
                   .HasForeignKey(e => e.CategoryId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
