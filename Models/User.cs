using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; } // HINT: In a real app, hash this password!

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // e.g., "Admin", "Doctor", "Patient"

        // Nullable foreign key to link to Patient (if this user is a patient)
        [ForeignKey("Patient")]
        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }

        // Nullable foreign key to link to Doctor (if this user is a doctor)
        [ForeignKey("Doctor")]
        public int? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
