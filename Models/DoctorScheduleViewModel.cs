using System.Collections.Generic;

namespace HospitalManagementSystem.Models
{
    // This ViewModel is designed to pass all necessary data to the Doctor's Schedule view
    public class DoctorScheduleViewModel
    {
        public Doctor Doctor { get; set; } // The doctor whose schedule is being viewed
        public DoctorSchedule DoctorSchedule { get; set; } // The general working hours for the day
        public List<string> DailyActivities { get; set; } = new List<string>(); // NEW: Random daily activities
        public List<Appointment> TodaysAppointments { get; set; } // Specific appointments for the day
    }
}
