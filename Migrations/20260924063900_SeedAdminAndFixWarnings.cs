using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NWSDB.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminAndFixWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 9, 24, 6, 38, 59, 417, DateTimeKind.Utc).AddTicks(5949), "$2a$11$qRzT3fU9zY8tM9o6G6o0xeO21r74jQ/hL505FkF9K7g8sU4vN.M8q" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 9, 24, 6, 34, 48, 222, DateTimeKind.Utc).AddTicks(2240), "$2a$11$kM19xNbkMWtvSZ1.onPPpOpUSB6cCZbn7Uu31a7UbkeuihoSTDCXq" });
        }
    }
}
