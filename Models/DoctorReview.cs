using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class DoctorReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; } // Navigation property

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; } // Navigation property

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReviewDate { get; set; }
    }
}