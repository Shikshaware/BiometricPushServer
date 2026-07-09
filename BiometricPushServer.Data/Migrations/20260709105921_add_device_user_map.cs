using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricPushServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_device_user_map : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BioDeviceUserMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioDeviceUserMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioDeviceUserMaps_BioDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "BioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BioDeviceUserMaps_BioUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BioUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceUserMaps_DeviceId",
                table: "BioDeviceUserMaps",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceUserMaps_DeviceId_UserId",
                table: "BioDeviceUserMaps",
                columns: new[] { "DeviceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceUserMaps_UserId",
                table: "BioDeviceUserMaps",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BioDeviceUserMaps");
        }
    }
}
