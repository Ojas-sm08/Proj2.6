using System;
using System.Collections.Generic; // Added for ICollection
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; } // e.g., Male, Female, Other

        [Required]
        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }

        [StringLength(100)]
        [EmailAddress] // Added EmailAddress attribute for validation
        public string? Email { get; set; } // Added Email property

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(1000)]
        [Display(Name = "Medical History")]
        public string? MedicalHistory { get; set; }

        // Navigation properties
        public ICollection<Appointment>? Appointments { get; set; }

        // NEW: Navigation property for Bills
        public ICollection<Bill> Bills { get; set; } = new List<Bill>(); // Initialize to prevent null reference

        // NEW: Navigation property for PatientFiles
        public ICollection<PatientFile>? PatientFiles { get; set; } // Patient can have many files
    }
}
