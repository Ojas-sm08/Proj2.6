// Models/User.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password hash is required.")]
        public string PasswordHash { get; set; } // Stores the HASHED password

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } // e.g., "Admin", "Doctor", "Patient"

        // Optional Foreign Key back to Doctor
        public int? DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } // Navigation property to Doctor

        // Optional Foreign Key back to Patient
        public int? PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } // Navigation property to Patient

        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}