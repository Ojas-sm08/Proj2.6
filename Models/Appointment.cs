using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Required for [ForeignKey]

namespace HospitalManagementSystem.Models
{
    public class Appointment
    {
        [Key] // This indicates 'Id' is the primary key
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; } // Navigation property to Patient

        [Required]
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; } // Navigation property to Doctor

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Appointment Date & Time")]
        public DateTime AppointmentDateTime { get; set; } // Combined Date and Time

        [StringLength(500)] // Corrected StringLength for Reason
        public string? Reason { get; set; }

        [StringLength(100)] // Corrected StringLength for Location
        public string? Location { get; set; } // Where the appointment takes place

        [StringLength(50)] // Added StringLength for Status
        public string Status { get; set; } = "Scheduled"; // Default status for new appointments

        public ICollection<Bill>? Bills { get; set; } = new List<Bill>();
    }
}
