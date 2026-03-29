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
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.HasKey(pt => pt.Id);
            builder.Property(pt => pt.Id).ValueGeneratedNever();

            builder.Property(pt => pt.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(pt => pt.Status).HasConversion<string>();
            builder.Property(pt => pt.ExternalTransactionId).HasMaxLength(255);


            builder.HasOne(pt => pt.User)
                   .WithMany()
                   .HasForeignKey(pt => pt.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pt => pt.Event)
                   .WithMany()
                   .HasForeignKey(pt => pt.EventId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
