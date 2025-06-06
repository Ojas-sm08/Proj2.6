// Models/Appointment.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; } // Navigation property to Patient

        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; } // Navigation property to Doctor

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan Time { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public string? Status { get; set; } // e.g., "Scheduled", "Completed", "Cancelled"
    }
}