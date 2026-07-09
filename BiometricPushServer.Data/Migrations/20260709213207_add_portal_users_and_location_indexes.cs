using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricPushServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_portal_users_and_location_indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BioPortalUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    InviteToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InviteExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioPortalUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BioDevices_ClientId_LocationId",
                table: "BioDevices",
                columns: new[] { "ClientId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_BioDevices_LocationId",
                table: "BioDevices",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BioPortalUsers_ClientId_Username",
                table: "BioPortalUsers",
                columns: new[] { "ClientId", "Username" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BioPortalUsers_InviteToken",
                table: "BioPortalUsers",
                column: "InviteToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BioPortalUsers");

            migrationBuilder.DropIndex(
                name: "IX_BioDevices_ClientId_LocationId",
                table: "BioDevices");

            migrationBuilder.DropIndex(
                name: "IX_BioDevices_LocationId",
                table: "BioDevices");
        }
    }
}
