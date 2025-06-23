using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class newwest9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 22, 40, 254, DateTimeKind.Local).AddTicks(8339), new DateTime(2025, 6, 23, 13, 22, 40, 265, DateTimeKind.Local).AddTicks(9996) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 22, 40, 266, DateTimeKind.Local).AddTicks(1257), new DateTime(2025, 6, 23, 13, 22, 40, 266, DateTimeKind.Local).AddTicks(1262) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 22, 40, 266, DateTimeKind.Local).AddTicks(1294), new DateTime(2025, 6, 23, 13, 22, 40, 266, DateTimeKind.Local).AddTicks(1295) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 22, 40, 266, DateTimeKind.Local).AddTicks(1312), new DateTime(2025, 6, 23, 13, 22, 40, 266, DateTimeKind.Local).AddTicks(1313) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 216, DateTimeKind.Local).AddTicks(190), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(1372) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3462), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3469) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3616), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3618) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "LastLogin" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3685), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3687) });
        }
    }
}
