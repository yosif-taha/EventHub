using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentOrderCreationIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantOrderId",
                table: "PaymentTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OrderCreationAttempts",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OrderCreationFailureReason",
                table: "PaymentTransactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderCreationLastAttemptAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderCreationStatus",
                table: "PaymentTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Created");

            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                table: "PaymentTransactions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // Existing orders used the registration ID as Paymob merchant_order_id.
            migrationBuilder.Sql("UPDATE [PaymentTransactions] SET [MerchantOrderId] = CONVERT(nvarchar(36), [RegistrationId]) WHERE [MerchantOrderId] = ''");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MerchantOrderId",
                table: "PaymentTransactions",
                column: "MerchantOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_MerchantOrderId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(name: "MerchantOrderId", table: "PaymentTransactions");
            migrationBuilder.DropColumn(name: "OrderCreationAttempts", table: "PaymentTransactions");
            migrationBuilder.DropColumn(name: "OrderCreationFailureReason", table: "PaymentTransactions");
            migrationBuilder.DropColumn(name: "OrderCreationLastAttemptAt", table: "PaymentTransactions");
            migrationBuilder.DropColumn(name: "OrderCreationStatus", table: "PaymentTransactions");
            migrationBuilder.DropColumn(name: "PaymentUrl", table: "PaymentTransactions");
        }
    }
}
