using EventHub.Domin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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


            builder.HasOne(pt => pt.Registration)
                   .WithMany(u => u.PaymentTransactions)
                   .HasForeignKey(pt => pt.RegistrationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
