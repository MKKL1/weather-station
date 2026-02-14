using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHmacSecretFromHomeServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HmacSecret",
                table: "Devices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HmacSecret",
                table: "Devices",
                type: "text",
                nullable: true);
        }
    }
}
