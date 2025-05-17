using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace HospitalManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        private static List<Doctor> doctors = new List<Doctor>
        {
            new Doctor { Id = 1, Name = "Dr. Aditi Sharma", Specialization = "Cardiologist", Description = "Heart specialist", Contact = "aditi@example.com", Location = "A101", Schedules = new(), Reviews = new() },
            new Doctor { Id = 2, Name = "Dr. Pranav Joshi", Specialization = "Cardiologist", Description = "Expert in cardiac surgery", Contact = "pranav@example.com", Location = "A102", Schedules = new(), Reviews = new() },
            new Doctor { Id = 3, Name = "Dr. Rohan Mehta", Specialization = "Dermatologist", Description = "Skin specialist", Contact = "rohan@example.com", Location = "B202", Schedules = new(), Reviews = new() },
            new Doctor { Id = 4, Name = "Dr. Kavya Nair", Specialization = "Dermatologist", Description = "Hair & skin care expert", Contact = "kavya@example.com", Location = "B203", Schedules = new(), Reviews = new() },
            new Doctor { Id = 5, Name = "Dr. Ananya Rao", Specialization = "Pediatrician", Description = "Child health expert", Contact = "ananya@example.com", Location = "C303", Schedules = new(), Reviews = new() },
            new Doctor { Id = 6, Name = "Dr. Sudeep Das", Specialization = "Pediatrician", Description = "Newborn care specialist", Contact = "sudeep@example.com", Location = "C304", Schedules = new(), Reviews = new() },
            new Doctor { Id = 7, Name = "Dr. Sneha Kapoor", Specialization = "Neurologist", Description = "Brain & spine specialist", Contact = "sneha@example.com", Location = "D404", Schedules = new(), Reviews = new() },
            new Doctor { Id = 8, Name = "Dr. Rajan Pillai", Specialization = "Neurologist", Description = "Neurotherapist", Contact = "rajan@example.com", Location = "D405", Schedules = new(), Reviews = new() },
            new Doctor { Id = 9, Name = "Dr. Priya Iyer", Specialization = "Gynaecologist", Description = "Women’s health consultant", Contact = "priya@example.com", Location = "E505", Schedules = new(), Reviews = new() },
            new Doctor { Id = 10, Name = "Dr. Fatima Sheikh", Specialization = "Gynaecologist", Description = "Prenatal & fertility care", Contact = "fatima@example.com", Location = "E506", Schedules = new(), Reviews = new() }
        };

        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Doctor")
                return RedirectToAction("Login", "Account");

            return View();
        }

        public IActionResult Index(string specialization)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role == "Doctor")
                return RedirectToAction("Dashboard");

            ViewBag.Specializations = doctors.Select(d => d.Specialization).Distinct().OrderBy(s => s).ToList();
            var filteredDoctors = string.IsNullOrWhiteSpace(specialization)
                ? doctors
                : doctors.Where(d => d.Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.SelectedSpecialization = specialization;
            return View(filteredDoctors);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Doctor doctor)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                doctor.Id = doctors.Count > 0 ? doctors.Max(d => d.Id) + 1 : 1;
                doctor.Schedules = new List<DoctorSchedule>();
                doctor.Reviews = new List<DoctorReview>();
                doctors.Add(doctor);

                TempData["Message"] = "Doctor added successfully!";
                return RedirectToAction("Index");
            }

            return View(doctor);
        }

        public IActionResult Schedule()
        {
            if (HttpContext.Session.GetString("Role") != "Doctor")
                return RedirectToAction("Login", "Account");

            var schedule = new List<DoctorSchedule>
            {
                new DoctorSchedule { Date = DateTime.Today, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0), Location = "Room 101" },
                new DoctorSchedule { Date = DateTime.Today.AddDays(1), StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Room 102" }
            };

            return View(schedule);
        }

        public IActionResult Billing()
        {
            if (HttpContext.Session.GetString("Role") != "Doctor")
                return RedirectToAction("Login", "Account");

            return View();
        }

        public IActionResult Patients()
        {
            if (HttpContext.Session.GetString("Role") != "Doctor")
                return RedirectToAction("Login", "Account");

            var patients = new List<Patient>
            {
                new Patient { PatientId = 1, Name = "John Doe", Gender = "Male", ContactNumber = "1234567890", DateOfBirth = new DateTime(1990, 1, 1) },
                new Patient { PatientId = 2, Name = "Jane Smith", Gender = "Female", ContactNumber = "9876543210", DateOfBirth = new DateTime(1985, 5, 5) }
            };

            return View(patients);
        }
    }
}
