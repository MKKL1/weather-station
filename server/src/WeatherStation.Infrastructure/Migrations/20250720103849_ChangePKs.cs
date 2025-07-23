using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherStation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangePKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_Name",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Devices");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Devices",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Id",
                table: "Devices",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_Id",
                table: "Devices");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Devices",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Devices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Name",
                table: "Devices",
                column: "Name",
                unique: true);
        }
    }
}
