using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; } // Primary key for Doctor

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Specialization { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Contact Info")]
        public string? Contact { get; set; } // Phone or Email

        [StringLength(100)]
        public string? Location { get; set; } // Clinic location or hospital wing

        // Navigation properties
        public ICollection<DoctorSchedule>? Schedules { get; set; } // Doctor can have multiple schedules
        public ICollection<DoctorReview>? Reviews { get; set; } // Optional: If you have a DoctorReview model
        public ICollection<Appointment>? Appointments { get; set; } // Optional: If you have an Appointment model

        // NEW: Navigation property for Bills
        // This links Doctor to the bills they create or are associated with.
        public ICollection<Bill> Bills { get; set; } = new List<Bill>(); // Initialize to prevent null reference

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        [ConcurrencyCheck] // Add ConcurrencyCheck for optimistic concurrency on username
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password hash is required.")]
        [StringLength(128)] // SHA256 produces 64 hex characters (256 bits), 128 for safety/future hashes
        public string PasswordHash { get; set; } = string.Empty;

        // NotMapped property for password input (not stored in DB directly)
        [NotMapped]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        // This is used for input only, not stored.
        // It's nullable because the user might not always change their password on edit.
        public string? NewPassword { get; set; }
    }
}
