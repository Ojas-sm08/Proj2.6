using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        // Predefined doctor list
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

        // GET: /Doctor/Index?specialization=xyz
        public IActionResult Index(string specialization)
        {
            // Get all specializations (unique)
            ViewBag.Specializations = doctors
                .Select(d => d.Specialization)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            // Filter doctors based on selection
            var filteredDoctors = string.IsNullOrWhiteSpace(specialization)
                ? doctors
                : doctors.Where(d =>
                      d.Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            ViewBag.SelectedSpecialization = specialization;
            return View(filteredDoctors);
        }

    }
}
