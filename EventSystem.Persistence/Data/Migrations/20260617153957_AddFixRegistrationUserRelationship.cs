using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFixRegistrationUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Users_ApplicationUserId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ApplicationUserId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Registrations");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Users_UserId",
                table: "Registrations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Users_UserId",
                table: "Registrations");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "Registrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ApplicationUserId",
                table: "Registrations",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Users_ApplicationUserId",
                table: "Registrations",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
