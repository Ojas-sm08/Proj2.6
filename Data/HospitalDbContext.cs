using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;

namespace HospitalManagementSystem.Data
{
    public class HospitalDbContext : DbContext
    {
        public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options) { }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<DoctorReview> DoctorReviews { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<HospitalManagementSystem.Models.PatientFile> PatientFiles { get; set; }
        public DbSet<HospitalManagementSystem.Models.Notification> Notifications { get; set; }
        // Add other DbSets if you have more models like Billing

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configure Relationships (Fluent API) ---

            // Doctor Relationships
            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Schedules)
                .WithOne(ds => ds.Doctor)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Reviews)
                .WithOne(dr => dr.Doctor)
                .HasForeignKey(dr => dr.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Appointments)
                .WithOne(a => a.Doctor)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting doctor if they have appointments

            // Patient Relationships
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // User-Patient Relationship (assuming User has a PatientId FK)
            modelBuilder.Entity<User>()
                .HasOne<Patient>() // A User may be associated with one Patient
                .WithOne() // A Patient may have one User account
                .HasForeignKey<User>(u => u.PatientId) // User has PatientId as FK
                .IsRequired(false) // PatientId can be null for Admin/Doctor users
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting patient if user account exists (optional, adjust as needed)


            // --- Seed Data ---

            // Seed data for Users - UPDATED with PatientId for patient accounts
            // IMPORTANT: Replace "admin123", "doc123", "pat123", "pat234" with actual hashed passwords in a real application!
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", PasswordHash = "admin123", Role = "Admin", PatientId = null },
                new User { Id = 2, Username = "doctor1", PasswordHash = "doc123", Role = "Doctor", PatientId = null },
                new User { Id = 3, Username = "patient1", PasswordHash = "pat123", Role = "Patient", PatientId = 1 }, // Link patient1 user to Patient with PatientId = 1 (Alice)
                new User { Id = 4, Username = "patient2", PasswordHash = "pat234", Role = "Patient", PatientId = 2 }  // Link patient2 user to Patient with PatientId = 2 (Bob)
            );

            // Seed data for Patients - UPDATED with descriptive names
            modelBuilder.Entity<Patient>().HasData(
                new Patient
                {
                    PatientId = 1,
                    Name = "Alice Wonderland",
                    DateOfBirth = new DateTime(1990, 5, 15),
                    Gender = "Female",
                    ContactNumber = "9876543210",
                    Address = "101 Maple St, City",
                    MedicalHistory = "No significant history.",
                    Email = "alice@example.com" // Added Email for consistency
                },
                new Patient
                {
                    PatientId = 2,
                    Name = "Bob The Builder",
                    DateOfBirth = new DateTime(1985, 8, 20),
                    Gender = "Male",
                    ContactNumber = "0123456789",
                    Address = "202 Oak Ave, Town",
                    MedicalHistory = "Seasonal allergies.",
                    Email = "bob@example.com" // Added Email for consistency
                },
                new Patient
                {
                    PatientId = 3,
                    Name = "Charlie Chaplin",
                    DateOfBirth = new DateTime(1975, 1, 1),
                    Gender = "Male",
                    ContactNumber = "555-1234",
                    Address = "789 Pine Ln, Village",
                    MedicalHistory = "Hypertension",
                    Email = "charlie@example.com" // Added Email for consistency
                }
            );

            // Seed data for Doctors - CORRECTED to match current Doctor model properties
            modelBuilder.Entity<Doctor>().HasData(
                new Doctor
                {
                    Id = 1, // Primary Key for Doctor
                    Name = "Dr. Smith",
                    Specialization = "Cardiology",
                    Description = "Expert in heart conditions.",
                    Contact = "smith@example.com",
                    Location = "Cardio Wing A101"
                },
                new Doctor
                {
                    Id = 2, // Primary Key for Doctor
                    Name = "Dr. Jones",
                    Specialization = "Pediatrics",
                    Description = "Specializes in child health.",
                    Contact = "jones@example.com",
                    Location = "Pediatric Ward C303"
                }
            );

            // Seed data for Appointments (STATIC DATES)
            modelBuilder.Entity<Appointment>().HasData(
                new Appointment
                {
                    Id = 1,
                    PatientId = 1,
                    DoctorId = 1,
                    AppointmentDateTime = new DateTime(2025, 6, 13, 10, 0, 0), // Use combined DateTime
                    Location = "Room 101",
                    Reason = "Annual Checkup",
                    Status = "Scheduled" // Added Status
                },
                new Appointment
                {
                    Id = 2,
                    PatientId = 2,
                    DoctorId = 2,
                    AppointmentDateTime = new DateTime(2025, 6, 16, 14, 30, 0), // Use combined DateTime
                    Location = "Room 202",
                    Reason = "Pediatric Consultation",
                    Status = "Scheduled" // Added Status
                }
            );

            // Seed data for DoctorReviews (STATIC DATES)
            modelBuilder.Entity<DoctorReview>().HasData(
                new DoctorReview
                {
                    Id = 1,
                    DoctorId = 1,
                    PatientId = 1,
                    Rating = 5,
                    Comment = "Excellent care from Dr. Smith!",
                    ReviewDate = new DateTime(2025, 6, 1),
                },
                new DoctorReview
                {
                    Id = 2,
                    DoctorId = 2,
                    PatientId = 2,
                    Rating = 4,
                    Comment = "Dr. Jones was very good with my child.",
                    ReviewDate = new DateTime(2025, 6, 4),
                }
            );

            // Seed data for DoctorSchedules (STATIC DATES)
            modelBuilder.Entity<DoctorSchedule>().HasData(
                new DoctorSchedule
                {
                    Id = 1,
                    DoctorId = 1,
                    Date = new DateTime(2025, 6, 7),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    Location = "Office A101"
                },
                new DoctorSchedule
                {
                    Id = 2,
                    DoctorId = 2,
                    Date = new DateTime(2025, 6, 8),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Location = "Clinic C303"
                }
            );
        }
    }
}
