// Models/Appointment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Patient is required.")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; } // Nullable reference type

        [Required(ErrorMessage = "Doctor is required.")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; } // Nullable reference type

        [Required(ErrorMessage = "Appointment Date and Time is required.")]
        [Display(Name = "Appointment Date and Time")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDateTime { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; } // Optional reason for appointment

        [StringLength(100)]
        public string? Location { get; set; } // e.g., "Clinic Room 3", "Telemedicine"

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50)]
        public string Status { get; set; } = "Scheduled"; // e.g., Scheduled, Completed, Cancelled

        // New Property: Price for the appointment itself
        [Required(ErrorMessage = "Price is required.")]
        [Column(TypeName = "decimal(18, 2)")] // Ensure proper decimal precision in DB
        [Range(0.00, 1000000.00, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; } = 0.00m; // Default to 0.00

        public ICollection<Bill>? Bills { get; set; } = new List<Bill>();

        // Navigation property for Bill (one-to-one or one-to-many depending on design)
        // If an appointment can only have one bill, you might not need a collection here.
        // If a bill can be created for an appointment, and that's the only linkage
        // then the foreign key might be on the Bill model, referencing AppointmentId.
        // For simplicity, we'll assume a Bill is created *from* an Appointment.
        // If Bill has an AppointmentId foreign key, then this navigation property is optional here.
        // If you want a direct link from Appointment to Bill (1-to-1), you'd define:
        // public Bill Bill { get; set; }
    }
}
