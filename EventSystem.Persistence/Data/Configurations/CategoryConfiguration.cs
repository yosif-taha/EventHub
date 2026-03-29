using EventHub.Domin.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Persistence.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<EventCategory>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<EventCategory> builder)
        {

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever();

            builder.Property(c => c.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.HasIndex(c => c.Name).IsUnique();


            builder.HasMany(c => c.Events)
                   .WithOne(e => e.Category)
                   .HasForeignKey(e => e.CategoryId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
