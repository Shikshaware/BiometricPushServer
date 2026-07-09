using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricPushServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_portal_user_timezone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "BioPortalUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "BioPortalUsers");
        }
    }
}
