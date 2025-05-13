using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Specialization { get; set; }

        public string Description { get; set; }
        public string Contact { get; set; }
        public string Location { get; set; }

        public List<DoctorSchedule> Schedules { get; set; }
        public List<DoctorReview> Reviews { get; set; }
    }

    public class DoctorSchedule
    {
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
    }

    public class DoctorReview
    {
        public string PatientName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}
