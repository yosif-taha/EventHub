using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentRequired",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "StripePaymentIntentId",
                table: "PaymentTransactions",
                newName: "PaymobTransactionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "PaymobOrderId",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Events",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymobOrderId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "PaymobTransactionId",
                table: "PaymentTransactions",
                newName: "StripePaymentIntentId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentRequired",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
