using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Hospital_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class one : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Time = table.Column<TimeSpan>(type: "time", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorReviews_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorReviews_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "Contact", "Description", "Location", "Name", "Specialization" },
                values: new object[,]
                {
                    { 1, "smith@example.com", "Expert in heart conditions.", "Cardio Wing A101", "Dr. Smith", "Cardiology" },
                    { 2, "jones@example.com", "Specializes in child health.", "Pediatric Ward C303", "Dr. Jones", "Pediatrics" }
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "PatientId", "Address", "ContactNumber", "DateOfBirth", "Gender", "MedicalHistory", "Name" },
                values: new object[,]
                {
                    { 1, "101 Maple St, City", "9876543210", new DateTime(1990, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "No significant history.", "patient1" },
                    { 2, "202 Oak Ave, Town", "0123456789", new DateTime(1985, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Female", "Seasonal allergies.", "patient2" },
                    { 3, "789 Pine Ln, Village", "555-1234", new DateTime(1975, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male", "Hypertension", "John Doe" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin123", "Admin", "admin" },
                    { 2, "doc123", "Doctor", "doctor1" },
                    { 3, "pat123", "Patient", "patient1" },
                    { 4, "pat234", "Patient", "patient2" }
                });

            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "Date", "DoctorId", "Location", "PatientId", "Reason", "Status", "Time" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Room 101", 1, "Annual Checkup", null, new TimeSpan(0, 10, 0, 0, 0) },
                    { 2, new DateTime(2025, 6, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Room 202", 2, "Pediatric Consultation", null, new TimeSpan(0, 14, 30, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "DoctorReviews",
                columns: new[] { "Id", "Comment", "DoctorId", "PatientId", "Rating", "ReviewDate" },
                values: new object[,]
                {
                    { 1, "Excellent care from Dr. Smith!", 1, 1, 5, new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "Dr. Jones was very good with my child.", 2, 2, 4, new DateTime(2025, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "DoctorSchedules",
                columns: new[] { "Id", "Date", "DoctorId", "EndTime", "Location", "StartTime" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new TimeSpan(0, 13, 0, 0, 0), "Office A101", new TimeSpan(0, 9, 0, 0, 0) },
                    { 2, new DateTime(2025, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new TimeSpan(0, 16, 0, 0, 0), "Clinic C303", new TimeSpan(0, 10, 0, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReviews_DoctorId",
                table: "DoctorReviews",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReviews_PatientId",
                table: "DoctorReviews",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId",
                table: "DoctorSchedules",
                column: "DoctorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DoctorReviews");

            migrationBuilder.DropTable(
                name: "DoctorSchedules");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Doctors");
        }
    }
}
