using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Patients_PatientId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PatientId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "PatientId1",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "PatientId",
                keyValue: 1,
                column: "Email",
                value: "alice@example.com");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "PatientId",
                keyValue: 2,
                column: "Email",
                value: "bob@example.com");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "PatientId",
                keyValue: 3,
                column: "Email",
                value: "charlie@example.com");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PatientId1",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PatientId1",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PatientId", "PatientId1" },
                values: new object[] { 1, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PatientId", "PatientId1" },
                values: new object[] { 2, null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PatientId",
                table: "Users",
                column: "PatientId",
                unique: true,
                filter: "[PatientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PatientId1",
                table: "Users",
                column: "PatientId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Patients_PatientId",
                table: "Users",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Patients_PatientId1",
                table: "Users",
                column: "PatientId1",
                principalTable: "Patients",
                principalColumn: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Patients_PatientId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Patients_PatientId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PatientId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PatientId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PatientId1",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "PatientId",
                keyValue: 1,
                column: "Email",
                value: null);

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "PatientId",
                keyValue: 2,
                column: "Email",
                value: null);

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "PatientId",
                keyValue: 3,
                column: "Email",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PatientId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                column: "PatientId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PatientId",
                table: "Users",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Patients_PatientId",
                table: "Users",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientId");
        }
    }
}
