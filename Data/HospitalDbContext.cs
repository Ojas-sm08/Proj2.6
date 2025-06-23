using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;
using HospitalManagementSystem.Utility; // REQUIRED for PasswordHasher

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

        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<HospitalManagementSystem.Models.PatientFile> PatientFiles { get; set; }
        public DbSet<HospitalManagementSystem.Models.Notification> Notifications { get; set; }

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

            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Bills) // Doctor can issue many bills
                .WithOne(b => b.Doctor) // A bill is issued by one doctor
                .HasForeignKey(b => b.DoctorId)
                .IsRequired(false) // DoctorId can be null if a bill is not associated with a specific doctor (e.g., admin created it)
                .OnDelete(DeleteBehavior.SetNull); // If doctor is deleted, FK becomes null

            // Patient Relationships
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Bills) // Patient can have many bills
                .WithOne(b => b.Patient) // A bill belongs to one patient
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.PatientFiles) // Patient can have many patient files
                .WithOne(pf => pf.Patient)
                .HasForeignKey(pf => pf.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // User-Patient Relationship (One-to-One Optional: FK on User)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Patient) // A User has one (optional) Patient
                .WithOne() // Patient has one (optional) User
                .HasForeignKey<User>(u => u.PatientId) // PatientId is the FK on the User entity
                .IsRequired(false) // PatientId can be null for Admin/Doctor users
                .OnDelete(DeleteBehavior.Restrict); // Adjust as per your desired cascade behavior

            // User-Doctor Relationship (One-to-One Optional: FK on User)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Doctor) // A User has one (optional) Doctor
                .WithOne() // Doctor has one (optional) User
                .HasForeignKey<User>(u => u.DoctorId) // DoctorId is the FK on the User entity
                .IsRequired(false) // DoctorId can be null for Admin/Patient users
                .OnDelete(DeleteBehavior.Restrict); // Adjust as per your desired cascade behavior


            // BillItem Relationship (many-to-one to Bill)
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Bill)
                .WithMany(b => b.BillItems) // A Bill has many BillItems
                .HasForeignKey(bi => bi.BillId)
                .OnDelete(DeleteBehavior.Cascade); // If a bill is deleted, its items are also deleted.

            // Notification Relationship (assuming Notification has a UserId FK)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User) // A Notification belongs to one User
                .WithMany(u => u.Notifications) // A User can have many Notifications
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If user is deleted, their notifications are deleted


            // Configure composite key for DoctorSchedule
            modelBuilder.Entity<DoctorSchedule>()
                .HasKey(ds => ds.Id); // Assuming Id is now the primary key

            // --- Seed Data (Using Static Dates) ---

            // Seed data for Users - All passwords are now hashed
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", PasswordHash = PasswordHasher.HashPassword("admin@123"), Role = "Admin", DoctorId = null, PatientId = null, CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0), LastLogin = new DateTime(2023, 1, 1, 10, 0, 0) },
                new User { Id = 2, Username = "doctor1", PasswordHash = PasswordHasher.HashPassword("doc@123"), Role = "Doctor", DoctorId = 1, PatientId = null, CreatedAt = new DateTime(2023, 2, 1, 11, 0, 0), LastLogin = new DateTime(2023, 2, 1, 11, 0, 0) }, // Link to Doctor with Id 1
                new User { Id = 3, Username = "patient1", PasswordHash = PasswordHasher.HashPassword("pat@123"), Role = "Patient", DoctorId = null, PatientId = 1, CreatedAt = new DateTime(2023, 3, 1, 12, 0, 0), LastLogin = new DateTime(2023, 3, 1, 12, 0, 0) }, // Link to Patient with PatientId 1
                new User { Id = 4, Username = "patient2", PasswordHash = PasswordHasher.HashPassword("pat@234"), Role = "Patient", DoctorId = null, PatientId = 2, CreatedAt = new DateTime(2023, 4, 1, 13, 0, 0), LastLogin = new DateTime(2023, 4, 1, 13, 0, 0) }
            );

            // Seed data for Doctors
            modelBuilder.Entity<Doctor>().HasData(
                new Doctor
                {
                    Id = 1, // Primary Key for Doctor
                    Name = "Dr. Smith",
                    Specialization = "Cardiology",
                    Description = "Expert in heart conditions.",
                    Contact = "smith@example.com",
                    Location = "Cardio Wing A101",
                    UserId = 2 // Link to User with Id 2 ("doctor1")
                },
                new Doctor
                {
                    Id = 2, // Primary Key for Doctor
                    Name = "Dr. Jones",
                    Specialization = "Pediatrics",
                    Description = "Specializes in child health.",
                    Contact = "jones@example.com",
                    Location = "Pediatric Ward C303",
                    UserId = null // Dr. Jones currently has no linked user account in seed (optional, change if needed)
                }
            );

            // Seed data for Patients
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
                    Email = "alice@example.com"
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
                    Email = "bob@example.com"
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
                    Email = "charlie@example.com"
                }
            );

            // Seed data for Appointments (STATIC DATES)
            modelBuilder.Entity<Appointment>().HasData(
                new Appointment
                {
                    Id = 1,
                    PatientId = 1,
                    DoctorId = 1,
                    AppointmentDateTime = new DateTime(2025, 6, 13, 10, 0, 0), // Future date, but static for seed
                    Location = "Room 101",
                    Reason = "Annual Checkup",
                    Status = "Completed"
                },
                new Appointment
                {
                    Id = 2,
                    PatientId = 2,
                    DoctorId = 2,
                    AppointmentDateTime = new DateTime(2025, 6, 16, 14, 30, 0), // Future date, but static for seed
                    Location = "Room 202",
                    Reason = "Pediatric Consultation",
                    Status = "Scheduled"
                },
                new Appointment
                {
                    Id = 3,
                    PatientId = 3,
                    DoctorId = 1,
                    AppointmentDateTime = new DateTime(2025, 6, 12, 11, 0, 0), // Past date, static for seed
                    Location = "Room 101",
                    Reason = "Follow-up",
                    Status = "Completed"
                }
            );

            // Seed data for DoctorReviews
            modelBuilder.Entity<DoctorReview>().HasData(
                new DoctorReview
                {
                    Id = 1,
                    DoctorId = 1,
                    PatientId = 1,
                    Rating = 5,
                    Comment = "Excellent care from Dr. Smith!",
                    ReviewDate = new DateTime(2025, 6, 1), // Future date, but static for seed
                },
                new DoctorReview
                {
                    Id = 2,
                    DoctorId = 2,
                    PatientId = 2,
                    Rating = 4,
                    Comment = "Dr. Jones was very good with my child.",
                    ReviewDate = new DateTime(2025, 6, 4), // Future date, but static for seed
                }
            );

            // Seed data for DoctorSchedules
            modelBuilder.Entity<DoctorSchedule>().HasData(
                new DoctorSchedule
                {
                    Id = 1,
                    DoctorId = 1,
                    Date = new DateTime(2025, 6, 7), // Future date, but static for seed
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    Location = "Office A101"
                },
                new DoctorSchedule
                {
                    Id = 2,
                    DoctorId = 2,
                    Date = new DateTime(2025, 6, 8), // Future date, but static for seed
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Location = "Clinic C303"
                }
            );

            // Seed data for Bills
            modelBuilder.Entity<Bill>().HasData(
                new Bill
                {
                    BillId = 1,
                    AppointmentId = 1,
                    PatientId = 1,
                    DoctorId = 1,
                    BillDate = new DateTime(2025, 6, 13), // Future date, but static for seed
                    TotalAmount = 75.00m,
                    Status = "Paid",
                    Notes = "Routine checkup and basic tests."
                },
                new Bill
                {
                    BillId = 2,
                    AppointmentId = 3,
                    PatientId = 3,
                    DoctorId = 1,
                    BillDate = new DateTime(2025, 6, 12), // Past date, static for seed
                    TotalAmount = 50.00m,
                    Status = "Pending",
                    Notes = "Follow-up consultation."
                }
            );

            // Seed data for BillItems
            modelBuilder.Entity<BillItem>().HasData(
                new BillItem { BillItemId = 1, BillId = 1, ItemName = "Consultation Fee", Quantity = 1, UnitPrice = 50.00m, Amount = 50.00m },
                new BillItem { BillItemId = 2, BillId = 1, ItemName = "Basic Blood Work", Quantity = 1, UnitPrice = 25.00m, Amount = 25.00m },
                new BillItem { BillItemId = 3, BillId = 2, ItemName = "Consultation Fee", Quantity = 1, UnitPrice = 50.00m, Amount = 50.00m }
            );
        }
    }
}
