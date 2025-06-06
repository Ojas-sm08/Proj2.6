using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.EntityFrameworkCore; // Required for ToListAsync, FindAsync, Include, etc.
using HospitalManagementSystem.Data; // Required for HospitalDbContext

namespace HospitalManagementSystem.Controllers
{
    public class AppointmentController : Controller
    {
        // 1. Inject HospitalDbContext
        private readonly HospitalDbContext _context;

        public AppointmentController(HospitalDbContext context)
        {
            _context = context;
        }

        // Helper to check if logged in and if the role is allowed to make appointments
        private bool IsAuthorizedForAppointments()
        {
            var role = HttpContext.Session.GetString("Role");
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username")) &&
                   (role == "Patient" || role == "Admin"); // Only Patient or Admin can create appointments
        }

        // GET: /Appointment/Create - Displays the form to create an appointment
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAuthorizedForAppointments())
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch list of doctors from the database for the dropdown
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();

            // Patient ID should ideally come from the logged-in user session if it's a patient
            ViewBag.CurrentPatientId = HttpContext.Session.GetString("PatientId");

            return View();
        }

        // POST: /Appointment/Create - Handles form submission for creating an appointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,Date,Time,Location,Reason")] Appointment model)
        {
            if (!IsAuthorizedForAppointments())
            {
                return RedirectToAction("Login", "Account");
            }

            // Manually add PatientId if it's coming from a logged-in patient (and not explicitly in form)
            if (HttpContext.Session.GetString("Role") == "Patient" && model.PatientId == 0)
            {
                if (int.TryParse(HttpContext.Session.GetString("PatientId"), out int loggedInPatientId))
                {
                    model.PatientId = loggedInPatientId;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Could not determine patient ID from session. Please log in again.");
                    ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
                    return View(model);
                }
            }

            // Basic validation for Date (beyond model state)
            if (model.Date < DateTime.Today)
            {
                ModelState.AddModelError("Date", "Appointment date cannot be in the past.");
            }

            var selectedDoctor = await _context.Doctors.FindAsync(model.DoctorId);
            if (selectedDoctor == null)
            {
                ModelState.AddModelError("DoctorId", "Selected doctor is not valid.");
            }
            else
            {
                // Basic check for available slots based on DoctorSchedules
                var doctorSchedule = await _context.DoctorSchedules
                                                   .Where(ds => ds.DoctorId == model.DoctorId && ds.Date.Date == model.Date.Date)
                                                   .FirstOrDefaultAsync();

                if (doctorSchedule == null || model.Time < doctorSchedule.StartTime || model.Time >= doctorSchedule.EndTime)
                {
                    ModelState.AddModelError("Time", $"Dr. {selectedDoctor.Name} is not available at {model.Time:hh\\:mm} on {model.Date.ToShortDateString()}.");
                }
                else
                {
                    // Check for overlapping appointments for the same doctor at the same time
                    bool hasOverlap = await _context.Appointments
                                                     .AnyAsync(a => a.DoctorId == model.DoctorId &&
                                                                    a.Date.Date == model.Date.Date &&
                                                                    a.Time == model.Time);
                    if (hasOverlap)
                    {
                        ModelState.AddModelError("Time", $"The selected time slot {model.Time:hh\\:mm} on {model.Date.ToShortDateString()} is already booked for Dr. {selectedDoctor.Name}.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment booked successfully!";
                return RedirectToAction("Success", new { id = model.Id });
            }

            // If ModelState is not valid, re-populate ViewBag for dropdowns
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
            ViewBag.CurrentPatientId = HttpContext.Session.GetString("PatientId");
            return View(model);
        }

        // GET: /Appointment/Success/{id} - Displays details of a newly created appointment
        public async Task<IActionResult> Success(int id)
        {
            if (!IsAuthorizedForAppointments())
            {
                return RedirectToAction("Login", "Account");
            }

            var appointment = await _context.Appointments
                                            .Include(a => a.Doctor) // Include Doctor details
                                            .Include(a => a.Patient) // Include Patient details
                                            .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: /Appointment/CheckAvailability - AJAX endpoint to get available slots for a doctor on a specific date
        [HttpGet]
        public async Task<JsonResult> CheckAvailability(int doctorId, DateTime date)
        {
            // Basic authentication check, consider more robust AJAX auth
            if (!IsAuthorizedForAppointments())
            {
                return Json(new { success = false, message = "Not authorized." });
            }

            var availableSlots = new List<TimeSpan>();

            // Find the doctor's schedule for the specific date
            var doctorSchedule = await _context.DoctorSchedules
                                               .Where(ds => ds.DoctorId == doctorId && ds.Date.Date == date.Date)
                                               .FirstOrDefaultAsync();

            if (doctorSchedule != null)
            {
                // Get all existing appointments for this doctor on this date
                var bookedAppointments = await _context.Appointments
                                                        .Where(a => a.DoctorId == doctorId && a.Date.Date == date.Date)
                                                        .Select(a => a.Time)
                                                        .ToListAsync();

                // Generate 30-minute slots within the doctor's working hours
                for (var time = doctorSchedule.StartTime; time < doctorSchedule.EndTime; time += TimeSpan.FromMinutes(30))
                {
                    // Add slot if it's not already booked
                    if (!bookedAppointments.Contains(time))
                    {
                        availableSlots.Add(time);
                    }
                }
            }

            return Json(new { success = true, slots = availableSlots.Select(t => t.ToString(@"hh\:mm")) });
        }
    }
}