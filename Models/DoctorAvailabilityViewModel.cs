using System;
using System.Collections.Generic;

namespace HospitalManagementSystem.Models
{
    // ViewModel to pass data for checking doctor availability
    public class DoctorAvailabilityViewModel
    {
        public List<Doctor> AllDoctors { get; set; } // List of all doctors to choose from
        public int? SelectedDoctorId { get; set; } // The ID of the doctor chosen by the user
        public DateTime SelectedDate { get; set; } // The date chosen by the user
        public List<TimeSpan> AvailableSlots { get; set; } = new List<TimeSpan>(); // List of available time slots
        public Doctor SelectedDoctorDetails { get; set; } // Details of the selected doctor
    }
}
