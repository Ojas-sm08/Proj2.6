using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));

        public IActionResult Index()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            string role = HttpContext.Session.GetString("Role");
            return role == "Patient" ? RedirectToAction("Dashboard") : RedirectToAction("Manage");
        }

        public IActionResult Dashboard()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View(); // Views/Patient/Dashboard.cshtml
        }

        public IActionResult Info()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var patient = new Patient
            {
                PatientId = 1,
                Name = "John Doe",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
                ContactNumber = "1234567890"
            };

            return View(patient); // Views/Patient/Info.cshtml
        }

        public IActionResult Billing()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            return View(); // Views/Patient/Billing.cshtml
        }

        public IActionResult Schedule()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var appointments = new List<Appointment>
            {
                new Appointment { Id = 1, DoctorName = "Dr. Aditi Sharma", PatientName = "John Doe", Date = DateTime.Today, Time = new TimeSpan(10, 30, 0), Location = "Room 201" },
                new Appointment { Id = 2, DoctorName = "Dr. Sneha Kapoor", PatientName = "John Doe", Date = DateTime.Today.AddDays(1), Time = new TimeSpan(14, 0, 0), Location = "Room 202" }
            };

            return View("Schedule", appointments); // Views/Patient/Schedule.cshtml
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            return View(); // Views/Patient/Create.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Appointment appointment)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                TempData["Message"] = "Appointment booked successfully!";
                return RedirectToAction("Create");
            }

            return View(appointment);
        }

        public IActionResult DoctorList()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var doctors = new List<Doctor>
            {
                new Doctor { Id = 1, Name = "Dr. Aditi Sharma", Specialization = "Cardiologist", Contact = "aditi@hospital.com", Location = "Cardio Wing A101" },
                new Doctor { Id = 2, Name = "Dr. Kavya Nair", Specialization = "Dermatologist", Contact = "kavya@hospital.com", Location = "Skin Dept B202" },
                new Doctor { Id = 3, Name = "Dr. Ananya Rao", Specialization = "Pediatrician", Contact = "ananya@hospital.com", Location = "Pediatric Ward C303" },
                new Doctor { Id = 4, Name = "Dr. Rajan Pillai", Specialization = "Neurologist", Contact = "rajan@hospital.com", Location = "Neuro Center D405" }
            };

            return View("DoctorList", doctors); // Views/Patient/DoctorList.cshtml
        }

        public IActionResult Appointments()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var appointments = new List<Appointment>
            {
                new Appointment { Id = 1, DoctorName = "Dr. Aditi Sharma", PatientName = "John Doe", Date = DateTime.Today, Time = new TimeSpan(10, 30, 0), Location = "Room 201" },
                new Appointment { Id = 2, DoctorName = "Dr. Sneha Kapoor", PatientName = "John Doe", Date = DateTime.Today.AddDays(1), Time = new TimeSpan(14, 0, 0), Location = "Room 202" }
            };

            return View("Appointments", appointments); // Views/Patient/Appointments.cshtml
        }

        public IActionResult Manage(string searchTerm, string gender)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var patients = new List<Patient>
            {
                new Patient { PatientId = 1, Name = "John Doe", DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male", ContactNumber = "1234567890" },
                new Patient { PatientId = 2, Name = "Jane Smith", DateOfBirth = new DateTime(1985, 5, 5), Gender = "Female", ContactNumber = "9876543210" },
                new Patient { PatientId = 3, Name = "Rahul Mehta", DateOfBirth = new DateTime(2000, 8, 15), Gender = "Male", ContactNumber = "9988776655" }
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                patients = patients.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.ContactNumber.Contains(searchTerm)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(gender) && gender != "All")
            {
                patients = patients.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View("Manage", patients); // Views/Patient/Manage.cshtml
        }

        public IActionResult Notifications()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            return View(); // Views/Patient/Notifications.cshtml
        }

        public IActionResult ProfilePreview()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            return View(); // Views/Patient/ProfilePreview.cshtml
        }
    }
}
