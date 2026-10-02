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
            builder.Property(pt => pt.MerchantOrderId).HasMaxLength(100).IsRequired();
            builder.Property(pt => pt.PaymentUrl).HasMaxLength(2000);
            builder.Property(pt => pt.OrderCreationStatus)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(EventHub.Domin.Enums.PaymentOrderCreationStatus.Pending);
            builder.Property(pt => pt.OrderCreationFailureReason).HasMaxLength(1000);
            builder.Property(pt => pt.RowVersion).IsRowVersion();
            builder.HasIndex(pt => pt.MerchantOrderId);
            builder.HasIndex(pt => pt.PaymobOrderId)
                   .IsUnique()
                   .HasFilter("[PaymobOrderId] IS NOT NULL");
            builder.HasIndex(pt => pt.PaymobTransactionId)
                   .IsUnique()
                   .HasFilter("[PaymobTransactionId] IS NOT NULL");


            builder.HasOne(pt => pt.Registration)
                   .WithMany(u => u.PaymentTransactions)
                   .HasForeignKey(pt => pt.RegistrationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
