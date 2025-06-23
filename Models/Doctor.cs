// Models/Doctor.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Needed for [NotMapped] and [ForeignKey]

namespace HospitalManagementSystem.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Doctor Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Specialization is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Specialization must be between 3 and 100 characters.")]
        public string Specialization { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } // Optional

        [Required(ErrorMessage = "Contact information is required.")]
        [StringLength(100, ErrorMessage = "Contact information cannot exceed 100 characters.")]
        public string Contact { get; set; } // e.g., email or phone

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string Location { get; set; }

        // Foreign Key to the User account associated with this doctor

        public ICollection<DoctorSchedule>? Schedules { get; set; } // Doctor can have multiple schedules
        public ICollection<DoctorReview>? Reviews { get; set; } // Optional: If you have a DoctorReview model
        public ICollection<Appointment>? Appointments { get; set; } // Optional: If you have an Appointment model

        // NEW: Navigation property for Bills
        // This links Doctor to the bills they create or are associated with.
        public ICollection<Bill> Bills { get; set; } = new List<Bill>(); // Initialize to prevent null reference
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } // Navigation property to the User model

        // This property is ONLY for the UI form (MyInfo) to capture a new password.
        // It is NOT mapped to a database column in the Doctor table.
        [NotMapped] // Important: Tells Entity Framework NOT to map this to a database column
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long.")]
        public string NewPassword { get; set; } // Used for password input in MyInfo view

        // IMPORTANT: DO NOT include Username or PasswordHash properties directly in Doctor.cs.
        // They belong solely to the User.cs model.
    }
}