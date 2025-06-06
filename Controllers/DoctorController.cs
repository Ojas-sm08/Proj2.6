using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
// Removed: using System.Diagnostics; // Debugging statements removed

namespace HospitalManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly HospitalDbContext _context;

        public DoctorController(HospitalDbContext context)
        {
            _context = context;
        }

        // Helper method to check if a user is logged in
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        // Helper method to check if the logged-in user is an Admin
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        // Helper method to check if the logged-in user is a Doctor
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";

        // GET: /Doctor/Dashboard (For logged-in doctors to see their dashboard)
        public async Task<IActionResult> Dashboard()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view your dashboard.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view your dashboard.";
                return RedirectToAction("Login", "Account");
            }

            // Retrieve DoctorId as a string and then parse it
            var doctorIdString = HttpContext.Session.GetString("DoctorId");

            if (int.TryParse(doctorIdString, out int loggedInDoctorId))
            {
                var doctor = await _context.Doctors
                                           .Include(d => d.Schedules)
                                           .Include(d => d.Appointments)
                                           .FirstOrDefaultAsync(d => d.Id == loggedInDoctorId);

                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor profile not found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }
                ViewData["Title"] = $"Doctor Dashboard - {doctor.Name}";
                return View(doctor);
            }
            else
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
        }

        // GET: /Doctor/Manage (Lists all doctors for Admin, with search and filter options)
        public async Task<IActionResult> Manage(string searchString, string specialization)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to manage doctors.";
                return RedirectToAction("Login", "Account");
            }

            IQueryable<Doctor> doctors = _context.Doctors;

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString) || (d.Contact != null && d.Contact.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(specialization) && specialization != "All")
            {
                doctors = doctors.Where(d => d.Specialization.ToLower() == specialization.ToLower());
            }

            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentSpecialization = specialization;

            var distinctSpecializations = await _context.Doctors
                                                        .Select(d => d.Specialization)
                                                        .Distinct()
                                                        .Where(s => s != null && s != "")
                                                        .OrderBy(s => s)
                                                        .ToListAsync();

            distinctSpecializations.Insert(0, "All");
            ViewBag.Specializations = distinctSpecializations;


            ViewData["Title"] = "Manage Doctors";
            return View("~/Views/Patient/DoctorList.cshtml", await doctors.ToListAsync());
        }

        // GET: Doctor/Create (for admin to add new doctor)
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to add new doctors.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Add New Doctor";
            return View();
        }

        // POST: Doctor/Create (for admin to add new doctor)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to add new doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                doctor.Schedules = doctor.Schedules ?? new List<DoctorSchedule>();
                doctor.Reviews = doctor.Reviews ?? new List<DoctorReview>();
                doctor.Appointments = doctor.Appointments ?? new List<Appointment>();

                _context.Add(doctor);
                await _context.SaveChangesAsync();

                string baseUsername = "dr." + doctor.Name.ToLower().Replace("dr.", "").Trim().Replace(" ", "");
                string generatedUsername = baseUsername;
                int suffix = 1;
                while (await _context.Users.AnyAsync(u => u.Username == generatedUsername))
                {
                    generatedUsername = $"{baseUsername}{suffix}";
                    suffix++;
                }

                string generatedPassword = generatedUsername; // INSECURE for production

                var newUser = new User
                {
                    Username = generatedUsername,
                    PasswordHash = generatedPassword,
                    Role = "Doctor"
                };

                _context.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Doctor '{doctor.Name}' added successfully!";
                TempData["NewDoctorLoginInfo"] = $"Login for Dr. {doctor.Name}: Username: {generatedUsername}, Password: {generatedPassword}. Please inform the doctor to change this immediately.";

                return RedirectToAction(nameof(Manage));
            }

            ViewData["Title"] = "Add New Doctor";
            return View(doctor);
        }

        // GET: Doctor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                                       .Include(d => d.Schedules)
                                       .Include(d => d.Reviews)
                                       .Include(d => d.Appointments)
                                       .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Doctor Details";
            return View(doctor);
        }

        // GET: Doctor/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to edit doctor details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Edit Doctor";
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to edit doctor details.";
                return RedirectToAction("Login", "Account");
            }

            if (id != doctor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Manage));
            }
            ViewData["Title"] = "Edit Doctor";
            return View(doctor);
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        // GET: Doctor/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Delete Doctor";
            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You must be logged in as an Admin to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor deleted successfully!";
            }
            return RedirectToAction(nameof(Manage));
        }

        // Schedule action (for a doctor to view their schedule)
        public async Task<IActionResult> Schedule()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view your schedule.";
                return RedirectToAction("Login", "Account");
            }

            var doctorIdString = HttpContext.Session.GetString("DoctorId"); // Get as string
            if (int.TryParse(doctorIdString, out int loggedInDoctorId)) // Parse string to int
            {
                var schedules = await _context.DoctorSchedules
                                              .Where(s => s.DoctorId == loggedInDoctorId)
                                              .ToListAsync();

                ViewData["Title"] = "My Schedule";
                return View(schedules);
            }
            else
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
        }

        // Billing action (for a doctor to view their billing info)
        public IActionResult Billing()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view your billing information.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "My Billing";
            return View();
        }

        // Patients action (for a doctor to view their patients)
        public async Task<IActionResult> Patients()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view your patients.";
                return RedirectToAction("Login", "Account");
            }

            var doctorIdString = HttpContext.Session.GetString("DoctorId"); // Get as string
            if (int.TryParse(doctorIdString, out int loggedInDoctorId)) // Parse string to int
            {
                var patients = await _context.Appointments
                                         .Where(a => a.DoctorId == loggedInDoctorId)
                                         .Select(a => a.Patient)
                                         .Distinct()
                                         .ToListAsync();

                ViewData["Title"] = "My Patients";
                return View(patients);
            }
            else
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
        }
    }
}
