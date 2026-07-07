using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricPushServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguredServerEndpointToDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfiguredServerAddress",
                table: "BioDevices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConfiguredServerPort",
                table: "BioDevices",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfiguredServerAddress",
                table: "BioDevices");

            migrationBuilder.DropColumn(
                name: "ConfiguredServerPort",
                table: "BioDevices");
        }
    }
}
