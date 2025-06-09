using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class DoctorSchedule
    {
        [Key] // Primary key for DoctorSchedule
        public int Id { get; set; }

        public int DoctorId { get; set; } // Foreign key to Doctor
        public Doctor? Doctor { get; set; } // Navigation property back to Doctor

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        public string? Location { get; set; } // Specific location for this schedule entry
        public bool IsAvailable { get; set; } = true; // Indicates if the slot is open

        // New properties for defining typical working hours for a doctor
        // These can be used to generate random schedules
        public TimeSpan MinWorkTime { get; set; }
        public TimeSpan MaxWorkTime { get; set; }
        public TimeSpan LunchStartTime { get; set; }
        public TimeSpan LunchEndTime { get; set; }
    }
}
