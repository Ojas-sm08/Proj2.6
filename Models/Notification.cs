using System;
using System.Collections.Generic; // Added for ICollection (though not strictly needed here, good for consistency)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        // Foreign Keys for who the notification is FOR (nullable if not applicable)
        public int? PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        public int? DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; }

        public int? AdminId { get; set; } // If you have an Admin model
        // [ForeignKey("AdminId")]
        // public Admin? Admin { get; set; }

        // NEW: Foreign Key and Navigation Property for User (general user association)
        public int? UserId { get; set; } // Can be nullable if notifications are not always user-specific
        [ForeignKey("UserId")]
        public User? User { get; set; } // Assuming you have a 'User' model (or UserAccount, based on your DbContext)


        [StringLength(50)]
        [Display(Name = "Related Type")]
        public string? RelatedType { get; set; } // e.g., "Appointment", "Lab Result", "Message"

        public int? RelatedId { get; set; } // ID of the related entity (e.g., AppointmentId, LabResultId)
    }
}
