using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Controllers
{
    public class AppointmentController : Controller
    {
        // Mock appointment list
        private static List<Appointment> appointments = new List<Appointment>
        {
            new Appointment { Id = 1, DoctorName = "Dr. Smith", PatientName = "John", Date = DateTime.Today, Time = new TimeSpan(10, 0, 0) }
        };

        // Doctor availability: each with available days and time slots
        private static Dictionary<string, List<(DayOfWeek Day, TimeSpan Start, TimeSpan End)>> doctorSchedule =
            new Dictionary<string, List<(DayOfWeek, TimeSpan, TimeSpan)>>
        {
            { "Dr. Smith", new List<(DayOfWeek, TimeSpan, TimeSpan)>
                {
                    (DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)),
                    (DayOfWeek.Wednesday, new TimeSpan(14, 0, 0), new TimeSpan(17, 0, 0)),
                    (DayOfWeek.Friday, new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0))
                }
            },
            { "Dr. Patel", new List<(DayOfWeek, TimeSpan, TimeSpan)>
                {
                    (DayOfWeek.Tuesday, new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0)),
                    (DayOfWeek.Thursday, new TimeSpan(15, 0, 0), new TimeSpan(18, 0, 0))
                }
            }
        };

        // GET: Appointment/Feature2 — Show form with doctor/date inputs
        public IActionResult Feature2()
        {
            ViewBag.Doctors = doctorSchedule.Keys.ToList();
            ViewBag.Slots = null; // no slots initially
            return View();
        }

        // POST: Appointment/Feature2 — Process and return available time slots
        [HttpPost]
        public IActionResult Feature2(string doctorName, DateTime date)
        {
            ViewBag.Doctors = doctorSchedule.Keys.ToList();
            ViewBag.SelectedDoctor = doctorName;
            ViewBag.SelectedDate = date.ToString("yyyy-MM-dd");

            if (string.IsNullOrEmpty(doctorName) || !doctorSchedule.ContainsKey(doctorName))
            {
                ViewBag.Error = "Please select a valid doctor.";
                return View();
            }

            var doctorSlots = doctorSchedule[doctorName]
                .Where(s => s.Day == date.DayOfWeek)
                .ToList();

            if (!doctorSlots.Any())
            {
                ViewBag.Slots = null;
                ViewBag.Error = $"❌ {doctorName} is not available on {date:dddd}.";
                return View();
            }

            var bookedTimes = appointments
                .Where(a => a.DoctorName == doctorName && a.Date.Date == date.Date)
                .Select(a => a.Time)
                .ToList();

            var availableSlots = new List<string>();
            foreach (var slot in doctorSlots)
            {
                for (var t = slot.Start; t <= slot.End; t = t.Add(TimeSpan.FromMinutes(30)))
                {
                    if (!bookedTimes.Contains(t))
                    {
                        availableSlots.Add(t.ToString(@"hh\:mm"));
                    }
                }
            }

            ViewBag.Slots = availableSlots;
            return View();
        }
    }
}
