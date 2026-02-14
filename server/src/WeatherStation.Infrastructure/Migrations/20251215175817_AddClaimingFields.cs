using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HmacSecret",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Devices",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HmacSecret",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Devices");
        }
    }
}
