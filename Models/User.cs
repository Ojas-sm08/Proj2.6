using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        public string? PasswordHash { get; set; } // Store hashed passwords!

        [Required]
        public string? Role { get; set; }
    }
}