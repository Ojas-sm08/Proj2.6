using System;
using System.Collections.Generic;

namespace HospitalManagementSystem.Models
{
    public class DoctorScheduleViewModel
    {
        public Doctor? Doctor { get; set; } // The doctor whose schedule is being viewed
        public DoctorSchedule? DoctorSchedule { get; set; } // The doctor's general working hours/location for a specific day
        public List<Appointment>? TodaysAppointments { get; set; } = new List<Appointment>(); // List of actual appointments for the day
        public List<string>? DailyActivities { get; set; } = new List<string>(); // Other non-appointment activities
    }
}
