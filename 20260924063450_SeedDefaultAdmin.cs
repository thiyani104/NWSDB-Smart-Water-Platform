using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NWSDB.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "PasswordHash", "Phone", "Role", "Username" },
                values: new object[] { 1, new DateTime(2026, 9, 24, 6, 34, 48, 222, DateTimeKind.Utc).AddTicks(2240), "admin@nwsdb.lk", "$2a$11$kM19xNbkMWtvSZ1.onPPpOpUSB6cCZbn7Uu31a7UbkeuihoSTDCXq", "0112635888", "Admin", "nwsdb_admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
