using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        // Patient Management + Search/Filter
        public async Task<IActionResult> Index(string searchTerm, string gender)
        {
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

            return View(patients);
        }

        // Notifications View
        public IActionResult Notifications()
        {
            return View(); // Views/Patient/Notifications.cshtml
        }

        // Profile Preview View
        public IActionResult ProfilePreview()
        {
            return View(); // Views/Patient/ProfilePreview.cshtml
        }
    }
}
