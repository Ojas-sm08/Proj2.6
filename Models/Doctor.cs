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
    }
}
