using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class PatientSignUpViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full Name must be between 3 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s.]+$", ErrorMessage = "Full Name can only contain letters, spaces, and periods.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Contact Number is required.")]
        [Phone(ErrorMessage = "Invalid Contact Number.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Contact Number must be between 10 and 15 digits.")]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; }

        public string MedicalHistory { get; set; } = "No significant history."; // Default value
    }
}
