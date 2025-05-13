using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string? Name { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? ContactNumber { get; set; }

        public string? Address { get; set; }

        public string? MedicalHistory { get; set; }
    }
}
