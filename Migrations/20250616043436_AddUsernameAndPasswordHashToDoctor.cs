using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameAndPasswordHashToDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Doctors",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Doctors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Doctors");
        }
    }
}
