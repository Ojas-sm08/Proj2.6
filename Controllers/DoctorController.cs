using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Globalization;

namespace HospitalManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly HospitalDbContext _context;

        public DoctorController(HospitalDbContext context)
        {
            _context = context;
        }

        // Helper methods for session/role checks
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";
        private bool IsPatient() => HttpContext.Session.GetString("Role") == "Patient";

        // GET: Doctor/Dashboard
        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Doctor")
            {
                TempData["ErrorMessage"] = "You must be logged in as a doctor to access the dashboard.";
                return RedirectToAction("Login", "Account", new { role = "Doctor" });
            }

            ViewData["Title"] = "Doctor Dashboard";
            ViewBag.Username = username?.Replace("dr.", ""); // Display without prefix
            Debug.WriteLine($"DoctorController.Dashboard: User '{username}' ({role}) accessed dashboard.");
            return View();
        }

        // GET: Doctor/Manage (Admin View)
        [HttpGet]
        public async Task<IActionResult> Manage(string searchString, string specialization)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to manage doctors.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Manage Doctors";
            IQueryable<Doctor> doctors = _context.Doctors;

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString) || d.Contact.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(specialization))
            {
                doctors = doctors.Where(d => d.Specialization == specialization);
            }

            ViewBag.Specializations = await _context.Doctors
                                                    .Select(d => d.Specialization)
                                                    .Where(s => !string.IsNullOrEmpty(s))
                                                    .Distinct()
                                                    .OrderBy(s => s)
                                                    .ToListAsync();

            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentSpecialization = specialization;

            return View(await doctors.ToListAsync());
        }

        // GET: Doctor/Create (Admin)
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to add new doctors.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Add New Doctor";
            return View();
        }

        // POST: Doctor/Create (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to add new doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();

                var doctorUsername = $"dr.{doctor.Name.Replace(" ", "").ToLower()}";

                var newDoctorUser = new User
                {
                    Username = doctorUsername,
                    PasswordHash = doctorUsername,
                    Role = "Doctor",
                    DoctorId = doctor.Id
                };
                _context.Add(newDoctorUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Doctor {doctor.Name} added successfully!";
                TempData["NewDoctorLoginInfo"] = $"New doctor user created: Username: {newDoctorUser.Username}, Password: {newDoctorUser.PasswordHash} (Please change this in a real application!)";
                return RedirectToAction(nameof(Manage));
            }
            ViewData["Title"] = "Add New Doctor";
            return View(doctor);
        }

        // GET: Doctor/Edit/5 (Admin or Doctor editing their own profile)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to edit doctor profiles.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            var currentUserId = HttpContext.Session.GetInt32("DoctorId");
            var currentUserRole = HttpContext.Session.GetString("Role");

            if (currentUserRole == "Admin")
            {
                // Admin can edit any doctor
            }
            else if (currentUserRole == "Doctor")
            {
                if (id != currentUserId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this doctor's profile.";
                    return RedirectToAction("Dashboard", "Doctor");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "You are not authorized to edit doctor profiles.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = $"Edit Doctor: {doctor.Name}";
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to edit doctor profiles.";
                return RedirectToAction("Login", "Account");
            }

            if (id != doctor.Id) return NotFound();

            var currentUserId = HttpContext.Session.GetInt32("DoctorId");
            var currentUserRole = HttpContext.Session.GetString("Role");

            if (currentUserRole == "Admin")
            {
                // Admin can edit any doctor
            }
            else if (currentUserRole == "Doctor")
            {
                if (id != currentUserId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this doctor's profile.";
                    return RedirectToAction("Dashboard", "Doctor");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "You are not authorized to edit doctor profiles.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Doctor {doctor.Name} updated successfully!";
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
            ViewData["Title"] = $"Edit Doctor: {doctor.Name}";
            return View(doctor);
        }

        // GET: Doctor/Details/5 (Admin, Doctor, or Patient viewing details)
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view doctor details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                                     .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            ViewData["Title"] = $"Doctor Details: {doctor.Name}";
            return View(doctor);
        }

        // GET: Doctor/Delete/5 (Admin)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                                     .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            ViewData["Title"] = $"Delete Doctor: {doctor.Name}";
            return View(doctor);
        }

        // POST: Doctor/Delete/5 (Admin)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized to delete doctors." });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                    {
                        return Json(new { success = false, message = "Doctor not found." });
                    }

                    // Check for associated appointments
                    var appointments = await _context.Appointments
                                                     .Where(a => a.DoctorId == doctor.Id)
                                                     .ToListAsync();
                    if (appointments.Any())
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"Cannot delete doctor {doctor.Name}. There are {appointments.Count} existing appointments linked to this doctor. Please delete or reassign appointments first." });
                    }

                    // Delete associated schedules
                    var schedules = await _context.DoctorSchedules.Where(ds => ds.DoctorId == doctor.Id).ToListAsync();
                    if (schedules.Any())
                    {
                        _context.DoctorSchedules.RemoveRange(schedules);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted {schedules.Count} schedules for doctor ID: {id}");
                    }

                    // Delete associated reviews
                    var reviews = await _context.DoctorReviews.Where(dr => dr.DoctorId == doctor.Id).ToListAsync();
                    if (reviews.Any())
                    {
                        _context.DoctorReviews.RemoveRange(reviews);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted {reviews.Count} reviews for doctor ID: {id}");
                    }

                    // Delete associated user account
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == doctor.Id);
                    if (user != null)
                    {
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted associated user for doctor ID: {id}, Username: {user.Username}");
                    }
                    else
                    {
                        Debug.WriteLine($"No associated user found for doctor ID: {id}");
                    }

                    _context.Doctors.Remove(doctor);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    return Json(new { success = true, message = $"Doctor {doctor.Name} and associated data deleted successfully!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"Error deleting doctor: {ex.Message}");
                    return Json(new { success = false, message = $"Error deleting doctor: {ex.Message}" });
                }
            }
        }

        // UPDATED: GET: Doctor/Schedule (Doctor's Own Schedule with Date Filter)
        [HttpGet]
        public async Task<IActionResult> Schedule(DateTime? selectedDate) // Added nullable DateTime parameter
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a doctor to view your schedule.";
                return RedirectToAction("Login", "Account");
            }

            int? doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (!doctorId.HasValue || doctorId.Value == 0)
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor profile not found.";
                return RedirectToAction("Dashboard", "Doctor");
            }

            // If selectedDate is null, default to today
            DateTime dateToView = selectedDate?.Date ?? DateTime.Today.Date;

            // Fetch the doctor's schedule for the selected date (or default if not set)
            var doctorSchedule = await _context.DoctorSchedules
                                                .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId.Value && ds.Date.Date == dateToView.Date);

            // Fetch appointments for the logged-in doctor for the selected date
            var appointmentsForDate = await _context.Appointments
                                                    .Include(a => a.Patient)
                                                    .Where(a => a.DoctorId == doctorId.Value && a.AppointmentDateTime.Date == dateToView.Date)
                                                    .OrderBy(a => a.AppointmentDateTime)
                                                    .ToListAsync();

            ViewData["Title"] = "My Schedule";
            ViewData["CurrentSelectedDate"] = dateToView.ToString("yyyy-MM-dd"); // Pass selected date to view for date picker value

            var viewModel = new DoctorScheduleViewModel
            {
                Doctor = doctor,
                DoctorSchedule = doctorSchedule,
                TodaysAppointments = appointmentsForDate, // Renamed to be more general
                DailyActivities = new List<string> { "Morning Rounds", "Patient Consultations", "Admin Work" }
            };

            return View(viewModel);
        }


        // GET: Doctor/Patients (Implemented with data fetching and Gender Filter)
        [HttpGet]
        public async Task<IActionResult> Patients(string searchString, string genderFilter)
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You are not authorized to view this page.";
                return RedirectToAction("Login", "Account");
            }

            int? doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (!doctorId.HasValue || doctorId.Value == 0)
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor profile not found.";
                return RedirectToAction("Dashboard", "Doctor");
            }

            var patientIdsForDoctor = await _context.Appointments
                                                    .Where(a => a.DoctorId == doctorId.Value)
                                                    .Select(a => a.PatientId)
                                                    .Distinct()
                                                    .ToListAsync();

            IQueryable<Patient> patientsQuery = _context.Patients
                                                        .Where(p => patientIdsForDoctor.Contains(p.PatientId));

            if (!string.IsNullOrEmpty(searchString))
            {
                patientsQuery = patientsQuery.Where(p => (p.Name != null && p.Name.Contains(searchString)) ||
                                                        (p.ContactNumber != null && p.ContactNumber.Contains(searchString)) ||
                                                        (p.Email != null && p.Email.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(genderFilter) && genderFilter != "All")
            {
                patientsQuery = patientsQuery.Where(p => p.Gender == genderFilter);
            }

            var patients = await patientsQuery.OrderBy(p => p.Name).ToListAsync();

            var patientDisplayList = new List<PatientDisplayViewModel>();
            foreach (var patient in patients)
            {
                var lastAppointment = await _context.Appointments
                                                    .Where(a => a.PatientId == patient.PatientId && a.DoctorId == doctorId.Value)
                                                    .OrderByDescending(a => a.AppointmentDateTime)
                                                    .FirstOrDefaultAsync();

                patientDisplayList.Add(new PatientDisplayViewModel
                {
                    PatientId = patient.PatientId,
                    Name = patient.Name ?? "N/A",
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender ?? "N/A",
                    ContactNumber = patient.ContactNumber ?? "N/A",
                    Email = patient.Email,
                    LastAppointmentDateTime = lastAppointment?.AppointmentDateTime
                });
            }

            var viewModel = new DoctorPatientsViewModel
            {
                Doctor = doctor,
                Patients = patientDisplayList
            };

            ViewData["Title"] = viewModel.Doctor?.Name != null ? $"{viewModel.Doctor.Name}'s Patients" : "My Patients";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentGenderFilter"] = genderFilter;

            return View(viewModel);
        }

        // GET: Doctor/Billing (Placeholder for Doctor's Billing)
        [HttpGet]
        public IActionResult Billing()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You are not authorized to view this page.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Doctor Billing";
            return View();
        }


        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }
    }
}
