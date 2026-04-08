using EventHub.Domin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Persistence.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.Id).ValueGeneratedNever(); // Guid V7 generated in code
            builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();

            builder.OwnsMany(u => u.RefreshTokens)
                   .WithOwner()
                   .HasForeignKey("UserId");
            
            builder.HasMany(u => u.CreatedEvents)
                   .WithOne(e => e.Organizer)
                   .HasForeignKey(e => e.OrganizerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
