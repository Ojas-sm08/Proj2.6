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

            // User-Patient Relationship (assuming User has a PatientId FK)
            modelBuilder.Entity<User>()
                .HasOne<Patient>() // A User may be associated with one Patient
                .WithOne() // A Patient may have one User account
                .HasForeignKey<User>(u => u.PatientId) // User has PatientId as FK
                .IsRequired(false) // PatientId can be null for Admin/Doctor users
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting patient if user account exists (optional, adjust as needed)

            // --- CORRECTED RELATIONSHIP: Appointment to Bill (One-to-Many) ---
            // An Appointment has many Bills, and a Bill belongs to one Appointment.
            modelBuilder.Entity<Appointment>()
                .HasMany(a => a.Bills)       // An Appointment has many Bills
                .WithOne(b => b.Appointment) // A Bill belongs to one Appointment
                .HasForeignKey(b => b.AppointmentId) // AppointmentId is the foreign key in Bill
                .OnDelete(DeleteBehavior.Cascade); // If an Appointment is deleted, its associated Bills (and their BillItems due to BillItem's cascade) are also deleted.

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
                .HasKey(ds => ds.Id); // Assuming Id is now the primary key, as in your DoctorSchedule model (if available)

            // --- Seed Data ---

            // Seed data for Users - UPDATED with PatientId for patient accounts
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
                    AppointmentDateTime = new DateTime(2025, 6, 13, 10, 0, 0),
                    Location = "Room 101",
                    Reason = "Annual Checkup",
                    Status = "Completed" // Set to Completed for potential billing
                },
                new Appointment
                {
                    Id = 2,
                    PatientId = 2,
                    DoctorId = 2,
                    AppointmentDateTime = new DateTime(2025, 6, 16, 14, 30, 0),
                    Location = "Room 202",
                    Reason = "Pediatric Consultation",
                    Status = "Scheduled"
                },
                 new Appointment
                 {
                     Id = 3,
                     PatientId = 3,
                     DoctorId = 1,
                     AppointmentDateTime = new DateTime(2025, 6, 12, 11, 0, 0),
                     Location = "Room 101",
                     Reason = "Follow-up",
                     Status = "Completed" // Set to Completed for potential billing
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
                    Id = 1, // Assuming Id is the PK, or use composite key if applicable
                    DoctorId = 1,
                    Date = new DateTime(2025, 6, 7),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    Location = "Office A101"
                },
                new DoctorSchedule
                {
                    Id = 2, // Assuming Id is the PK
                    DoctorId = 2,
                    Date = new DateTime(2025, 6, 8),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Location = "Clinic C303"
                }
            );

            // Seed data for Bills (NEW)
            modelBuilder.Entity<Bill>().HasData(
                new Bill
                {
                    BillId = 1,
                    AppointmentId = 1, // Linked to Alice's "Annual Checkup"
                    PatientId = 1,     // Alice
                    DoctorId = 1,      // Dr. Smith
                    BillDate = new DateTime(2025, 6, 13),
                    TotalAmount = 75.00m, // Will be overridden by items later
                    Status = "Paid",
                    Notes = "Routine checkup and basic tests."
                },
                new Bill
                {
                    BillId = 2,
                    AppointmentId = 3, // Linked to Charlie's "Follow-up"
                    PatientId = 3,     // Charlie
                    DoctorId = 1,      // Dr. Smith
                    BillDate = new DateTime(2025, 6, 12),
                    TotalAmount = 50.00m, // Will be overridden by items later
                    Status = "Pending",
                    Notes = "Follow-up consultation."
                }
            );

            // Seed data for BillItems (NEW)
            modelBuilder.Entity<BillItem>().HasData(
                new BillItem { BillItemId = 1, BillId = 1, ItemName = "Consultation Fee", Quantity = 1, UnitPrice = 50.00m, Amount = 50.00m },
                new BillItem { BillItemId = 2, BillId = 1, ItemName = "Basic Blood Work", Quantity = 1, UnitPrice = 25.00m, Amount = 25.00m },
                new BillItem { BillItemId = 3, BillId = 2, ItemName = "Consultation Fee", Quantity = 1, UnitPrice = 50.00m, Amount = 50.00m }
            );

            // IMPORTANT: If you have a separate UserAccount model, replace User with UserAccount if that's what you're using for auth.
            // public DbSet<UserAccount> UserAccounts { get; set; }
            // If you used UserAccount, change the User relationships and seed data accordingly.
            // I'm using "User" as per your provided context, but previously used "UserAccount".
            // Make sure the User model (or UserAccount model) matches the one you actually use.
        }
    }
}
