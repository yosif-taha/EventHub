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
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.Id).ValueGeneratedNever(); // Guid V7 generated in code
            builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();

            
            builder.HasMany(u => u.CreatedEvents)
                   .WithOne(e => e.Organizer)
                   .HasForeignKey(e => e.OrganizerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
