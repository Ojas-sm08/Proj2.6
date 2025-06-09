using System;
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

        // Navigation properties (if needed for relationships, e.g., Appointments)
        public ICollection<Appointment>? Appointments { get; set; }
    }
}
