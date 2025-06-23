using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class InitialDbSetupWithCorrectedSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Doctors_DoctorId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Patients_PatientId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DoctorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PatientId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PatientId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Doctors");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogin",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Doctors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contact",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Doctors",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                column: "UserId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                column: "UserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "LastLogin", "PasswordHash" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 216, DateTimeKind.Local).AddTicks(190), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(1372), "7676aaafb027c825bd9abab78b234070e702752f625b752e55e55b48e607e358" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DoctorId", "LastLogin", "PasswordHash" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3462), 1, new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3469), "05da31d4724854c42409e55c4e7f8e3eb3de79f08535851012a518aca15e8cc5" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "LastLogin", "PasswordHash" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3616), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3618), "573c5fe3730f043b0c85d0c8fa87aacd5c3e4bef144d96a4cb5863e2d879fb33" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "LastLogin", "PasswordHash" },
                values: new object[] { new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3685), new DateTime(2025, 6, 23, 13, 21, 22, 227, DateTimeKind.Local).AddTicks(3687), "a186de47b182db884fe11191bcd0e95624f2a008266705841d0892c3195a3436" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DoctorId",
                table: "Users",
                column: "DoctorId",
                unique: true,
                filter: "[DoctorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Users_UserId",
                table: "Doctors",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Doctors_DoctorId",
                table: "Users",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Users_UserId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Doctors_DoctorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DoctorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Doctors");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "PatientId1",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Doctors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Contact",
                table: "Doctors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PatientId1" },
                values: new object[] { "admin123", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DoctorId", "PasswordHash", "PatientId1" },
                values: new object[] { null, "doc123", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PatientId1" },
                values: new object[] { "pat123", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PatientId1" },
                values: new object[] { "pat234", null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DoctorId",
                table: "Users",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PatientId1",
                table: "Users",
                column: "PatientId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Doctors_DoctorId",
                table: "Users",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Patients_PatientId1",
                table: "Users",
                column: "PatientId1",
                principalTable: "Patients",
                principalColumn: "PatientId");
        }
    }
}
