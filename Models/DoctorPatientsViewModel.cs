using System;
using System.Collections.Generic;

namespace HospitalManagementSystem.Models
{
    // ViewModel to hold patient details specifically for display in the doctor's patient list
    // This class is now a top-level class within the Models namespace
    public class PatientDisplayViewModel
    {
        public int PatientId { get; set; }
        public string Name { get; set; } = string.Empty; // Initialize to prevent null warnings
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? Email { get; set; } // Nullable as per your Patient model
        public DateTime? LastAppointmentDateTime { get; set; } // Nullable, as a patient might not have a last appt
        // You could also add string? NextAppointmentDateTime { get; set; } here for future
    }

    public class DoctorPatientsViewModel
    {
        public Doctor? Doctor { get; set; } // The logged-in doctor
        // Now references the top-level PatientDisplayViewModel
        public List<PatientDisplayViewModel>? Patients { get; set; } = new List<PatientDisplayViewModel>(); // List of patients for display
    }
}
