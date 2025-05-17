using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string DoctorName { get; set; }

        [Required]
        public string PatientName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        // ✅ Add this if your view expects a TimeSlot field (optional alias)
        public string TimeSlot => $"{Time:hh\\:mm}";

        // ✅ Add this if your view expects a Location field
        [Required]
        public string Location { get; set; }
    }
}
