using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class PatientFile
    {
        [Key]
        public int Id { get; set; }

        // Make PatientId non-nullable as a file must belong to a patient
        [Required] // Explicitly mark as required
        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!; // Navigation property - make non-nullable

        [Required]
        [StringLength(255)]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        [Display(Name = "File Type")]
        public string? FileType { get; set; } // e.g., "PDF", "JPG", "Lab Report"

        [StringLength(500)]
        public string? Description { get; set; }

        // In a real application, you'd likely store a path or URL to the actual file storage
        // For now, this can be a placeholder or a reference for building dynamic URLs.
        [StringLength(1000)]
        public string? FilePath { get; set; }
    }
}
