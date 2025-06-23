// Models/PatientSignUpViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class PatientSignUpViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\.]+$", ErrorMessage = "Name can only contain letters, spaces, and periods.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Contact Number is required.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Contact Number must be exactly 10 digits.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact Number must be exactly 10 digits (digits only).")]
        [DataType(DataType.PhoneNumber)]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 200 characters.")]
        public string Address { get; set; }

        // Added as per your AccountController's usage
        public string MedicalHistory { get; set; }
    }
}