using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string? Gender { get; set; }

        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public string? MedicalHistory { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<DoctorReview> DoctorReviews { get; set; } = new List<DoctorReview>();
    }
}