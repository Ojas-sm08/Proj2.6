using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Hospital_Management_System.Models; // Removed or commented out if not explicitly needed and causing no errors.

namespace HospitalManagementSystem.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        // Link to the Appointment
        [Required] // This ensures AppointmentId is required
        public int AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; } // <<<< CHANGED: Made nullable for validation purposes

        // Link to the Patient
        [Required] // This ensures PatientId is required
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; } // <<<< CHANGED: Made nullable for validation purposes

        // Link to the Doctor who created the bill (optional, but good for tracking)
        public int? DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; } // Already nullable, no change needed here

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime BillDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // e.g., "Pending", "Paid", "Partially Paid", "Cancelled"

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; } // Made nullable as it's often optional

        // Navigation property for bill items
        public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }
}
