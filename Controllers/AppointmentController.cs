using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalManagementSystem.Controllers
{
    public class AppointmentController : Controller
    {
        private static List<Appointment> appointments = new List<Appointment>();

        // Doctor availability
        private static Dictionary<string, List<(DayOfWeek Day, TimeSpan Start, TimeSpan End)>> doctorSchedule =
            new Dictionary<string, List<(DayOfWeek, TimeSpan, TimeSpan)>>()
            {
                { "Dr. Smith", new List<(DayOfWeek, TimeSpan, TimeSpan)>
                    {
                        (DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)),
                        (DayOfWeek.Wednesday, new TimeSpan(14, 0, 0), new TimeSpan(17, 0, 0))
                    }
                }
            };

        // GET: /Appointment/Create
        public IActionResult Create()
        {
            ViewBag.Doctors = doctorSchedule.Keys.ToList();
            return View();
        }

        // POST: /Appointment/Create
        [HttpPost]
        public IActionResult Create(Appointment model)
        {
            if (ModelState.IsValid)
            {
                model.Id = appointments.Count + 1;
                appointments.Add(model);
                return RedirectToAction("Success", new { id = model.Id });
            }

            ViewBag.Doctors = doctorSchedule.Keys.ToList();
            return View(model);
        }

        public IActionResult Success(int id)
        {
            var appointment = appointments.FirstOrDefault(a => a.Id == id);
            return View(appointment);
        }

        // GET: /Appointment/Feature2
        public IActionResult Feature2()
        {
            ViewBag.Doctors = doctorSchedule.Keys.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Feature2(string doctor, DateTime date)
        {
            ViewBag.Doctors = doctorSchedule.Keys.ToList();
            var slots = new List<TimeSpan>();

            if (doctorSchedule.ContainsKey(doctor))
            {
                var availability = doctorSchedule[doctor]
                    .Where(d => d.Day == date.DayOfWeek)
                    .FirstOrDefault();

                if (availability != default)
                {
                    for (var time = availability.Start; time < availability.End; time += TimeSpan.FromMinutes(30))
                    {
                        if (!appointments.Any(a => a.DoctorName == doctor && a.Date == date && a.Time == time))
                        {
                            slots.Add(time);
                        }
                    }
                }
            }

            ViewBag.SelectedDoctor = doctor;
            ViewBag.SelectedDate = date.ToString("yyyy-MM-dd");
            ViewBag.AvailableSlots = slots;

            return View();
        }
    }
}
