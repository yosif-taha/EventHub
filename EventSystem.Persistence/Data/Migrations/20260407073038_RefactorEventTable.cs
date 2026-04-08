using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEventTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentAttendeesCount",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentAttendeesCount",
                table: "Events");
        }
    }
}
