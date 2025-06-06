using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Required for [Key] or [Required] if used

namespace HospitalManagementSystem.Models
{
    public class Doctor
    {
        // Primary Key for the Doctor entity
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Specialization { get; set; }

        // Properties for Description, Contact, and Location as requested
        public string? Description { get; set; } // Nullable string
        public string? Contact { get; set; }     // Nullable string
        public string? Location { get; set; }    // Nullable string

        // Navigation Properties: These link the Doctor to related entities.
        // They should be ICollection<T> or List<T> to represent a collection of related items.
        // Ensure that DoctorSchedule, DoctorReview, and Appointment models exist
        // and have a foreign key property (e.g., DoctorId) linking back to Doctor.Id.
        public List<DoctorSchedule>? Schedules { get; set; } // List of schedules for this doctor
        public List<DoctorReview>? Reviews { get; set; }     // List of reviews for this doctor
        public List<Appointment>? Appointments { get; set; }  // List of appointments for this doctor
    }

    // IMPORTANT: DoctorSchedule, DoctorReview, and Appointment models
    // should be defined in their OWN SEPARATE FILES within the Models folder.
    // E.g., Models/DoctorSchedule.cs, Models/DoctorReview.cs, Models/Appointment.cs.
    // The example structures were only for reference.
    // Do NOT keep nested definitions here if they are already separate files.
}
