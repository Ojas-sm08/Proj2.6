using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using System.Diagnostics;
using System; // For DateTime, TimeSpan
using System.Collections.Generic; // For List


namespace HospitalManagementSystem.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly HospitalDbContext _context;

        public AppointmentController(HospitalDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsPatient() => HttpContext.Session.GetString("Role") == "Patient";


        // GET: Appointment/AllAppointments (Admin and Doctor View)
        [HttpGet]
        public async Task<IActionResult> AllAppointments()
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to view all appointments.";
                return RedirectToAction("Login", "Account");
            }

            IQueryable<Appointment> appointments = _context.Appointments
                                                        .Include(a => a.Patient)
                                                        .Include(a => a.Doctor)
                                                        .OrderByDescending(a => a.AppointmentDateTime);

            if (IsDoctor())
            {
                int? doctorId = HttpContext.Session.GetInt32("DoctorId");
                if (!doctorId.HasValue || doctorId.Value == 0)
                {
                    TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }
                appointments = appointments.Where(a => a.DoctorId == doctorId.Value);
                ViewData["Title"] = "My Appointments";
            }
            else
            {
                ViewData["Title"] = "All Hospital Appointments";
            }

            return View(await appointments.ToListAsync());
        }

        // GET: Appointment/Appointments (Patient's Own Appointments)
        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            if (!IsLoggedIn() || !IsPatient())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Patient to view your appointments.";
                return RedirectToAction("Login", "Account");
            }

            int? patientId = HttpContext.Session.GetInt32("PatientId");
            if (!patientId.HasValue || patientId.Value == 0)
            {
                TempData["ErrorMessage"] = "Patient ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var patientAppointments = await _context.Appointments
                                                    .Include(a => a.Doctor)
                                                    .Where(a => a.PatientId == patientId.Value)
                                                    .OrderByDescending(a => a.AppointmentDateTime)
                                                    .ToListAsync();
            ViewData["Title"] = "My Appointments";
            return View(patientAppointments);
        }

        // GET: Appointment/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to book an appointment.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Patients = _context.Patients
                                .OrderBy(p => p.Name)
                                .Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" })
                                .ToList();
            ViewBag.Doctors = _context.Doctors
                               .OrderBy(d => d.Name)
                               .Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" })
                               .ToList();
            ViewData["Title"] = "Book New Appointment";
            return View();
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,AppointmentDateTime,Reason,Status")] Appointment appointment)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to book an appointment.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(appointment.Status))
                {
                    appointment.Status = "Scheduled"; // Default to Scheduled
                }

                bool isDoctorBusy = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == appointment.DoctorId &&
                    a.AppointmentDateTime == appointment.AppointmentDateTime &&
                    a.Status != "Cancelled" && a.Status != "Completed");

                if (isDoctorBusy)
                {
                    TempData["ErrorMessage"] = "The selected doctor is not available at this exact time. Please choose another time.";
                    ViewBag.Patients = _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToList();
                    ViewBag.Doctors = _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToList();
                    ViewData["Title"] = "Book New Appointment";
                    return View(appointment);
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment booked successfully!";
                return RedirectToAction(nameof(AllAppointments));
            }

            ViewBag.Patients = _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToList();
            ViewBag.Doctors = _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToList();
            ViewData["Title"] = "Book New Appointment";
            return View(appointment);
        }

        // GET: Appointment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit appointments.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            ViewBag.Patients = _context.Patients
                                .OrderBy(p => p.Name)
                                .Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" })
                                .ToList();
            ViewBag.Doctors = _context.Doctors
                               .OrderBy(d => d.Name)
                               .Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" })
                               .ToList();
            ViewData["Title"] = "Edit Appointment";
            return View(appointment);
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,AppointmentDateTime,Reason,Status")] Appointment appointment)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit appointments.";
                return RedirectToAction("Login", "Account");
            }

            if (id != appointment.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    bool isDoctorBusy = await _context.Appointments.AnyAsync(a =>
                        a.DoctorId == appointment.DoctorId &&
                        a.AppointmentDateTime == appointment.AppointmentDateTime &&
                        a.Id != appointment.Id &&
                        a.Status != "Cancelled" && a.Status != "Completed");

                    if (isDoctorBusy)
                    {
                        TempData["ErrorMessage"] = "The selected doctor is not available at this exact time. Please choose another time.";
                        ViewBag.Patients = _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToList();
                        ViewBag.Doctors = _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToList();
                        ViewData["Title"] = "Edit Appointment";
                        return View(appointment);
                    }

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Appointment updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(AllAppointments));
            }
            ViewBag.Patients = _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToList();
            ViewBag.Doctors = _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToList();
            ViewData["Title"] = "Edit Appointment";
            return View(appointment);
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // GET: Appointment/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to delete appointments.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                                            .Include(a => a.Patient)
                                            .Include(a => a.Doctor)
                                            .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();
            ViewData["Title"] = "Delete Appointment";
            return View(appointment);
        }

        // POST: Appointment/Delete/5 (Adjusted to return JsonResult for AJAX)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized to delete appointments." }); // Return JSON for AJAX
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found." }); // Return JSON for AJAX
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Appointment deleted successfully!" }); // Return JSON for AJAX
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting appointment: {ex.Message}");
                return Json(new { success = false, message = $"Error deleting appointment: {ex.Message}" }); // Return JSON for AJAX
            }
        }

        // POST: Appointment/MarkAsCompleted
        [HttpPost]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                return Json(new { success = false, message = "Unauthorized to mark as completed." }); // Return JSON for AJAX
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found." }); // Return JSON for AJAX
            }

            try
            {
                appointment.Status = "Completed";
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Appointment marked as completed." });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error marking appointment as completed: {ex.Message}");
                return Json(new { success = false, message = "Error marking appointment as completed.", error = ex.Message });
            }
        }

        // GET: Appointment/CheckAvailability (renders Views/Appointment/Feature2.cshtml)
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int? selectedDoctorId, DateTime? selectedDate)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to check doctor availability.";
                return RedirectToAction("Login", "Account");
            }

            var doctors = await _context.Doctors
                                        .OrderBy(d => d.Name)
                                        .Select(d => new Doctor { Id = d.Id, Name = d.Name, Specialization = d.Specialization })
                                        .ToListAsync();

            var viewModel = new DoctorAvailabilityViewModel
            {
                AllDoctors = doctors,
                SelectedDate = selectedDate ?? DateTime.Today,
                SelectedDoctorId = selectedDoctorId
            };

            ViewData["Title"] = "Check Doctor Availability";

            if (selectedDoctorId.HasValue && selectedDoctorId.Value > 0)
            {
                var doctor = await _context.Doctors.FindAsync(selectedDoctorId.Value);
                if (doctor != null)
                {
                    viewModel.SelectedDoctorDetails = doctor;

                    var doctorSchedule = await _context.DoctorSchedules
                                                       .FirstOrDefaultAsync(ds => ds.DoctorId == selectedDoctorId.Value && ds.Date.Date == viewModel.SelectedDate.Date);

                    if (doctorSchedule == null)
                    {
                        TempData["InfoMessage"] = $"No specific work schedule defined for Dr. {doctor.Name} on {viewModel.SelectedDate.ToShortDateString()}. Using default availability (9 AM - 5 PM, 12-1 PM lunch).";
                        doctorSchedule = new DoctorSchedule
                        {
                            StartTime = new TimeSpan(9, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            LunchStartTime = new TimeSpan(12, 0, 0),
                            LunchEndTime = new TimeSpan(13, 0, 0),
                            DoctorId = selectedDoctorId.Value,
                            Date = viewModel.SelectedDate.Date,
                            Location = "General Clinic"
                        };
                    }

                    var bookedAppointments = await _context.Appointments
                                                           .Where(a => a.DoctorId == selectedDoctorId.Value
                                                                    && a.AppointmentDateTime.Date == viewModel.SelectedDate.Date
                                                                    && a.Status != "Cancelled"
                                                                    && a.Status != "Completed")
                                                           .Select(a => a.AppointmentDateTime.TimeOfDay)
                                                           .ToListAsync();

                    TimeSpan slotDuration = TimeSpan.FromMinutes(30);

                    List<TimeSpan> availableSlots = new List<TimeSpan>();
                    if (doctorSchedule != null)
                    {
                        for (TimeSpan time = doctorSchedule.StartTime; time < doctorSchedule.EndTime; time = time.Add(slotDuration))
                        {
                            if ((time >= doctorSchedule.LunchStartTime && time < doctorSchedule.LunchEndTime) ||
                                (time.Add(slotDuration) > doctorSchedule.LunchStartTime && time.Add(slotDuration) <= doctorSchedule.LunchEndTime))
                            {
                                continue;
                            }

                            if (!bookedAppointments.Contains(time))
                            {
                                availableSlots.Add(time);
                            }
                        }
                    }
                    viewModel.AvailableSlots = availableSlots;
                }
            }
            return View("~/Views/Appointment/Feature2.cshtml", viewModel);
        }

        // AJAX endpoint to get available slots for Create Appointment page
        [HttpGet]
        public async Task<JsonResult> GetAvailableSlots(int doctorId, DateTime selectedDate)
        {
            if (doctorId <= 0 || selectedDate == DateTime.MinValue)
            {
                return Json(new { success = false, message = "Invalid doctor or date provided." });
            }

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor not found." });
            }

            var doctorSchedule = await _context.DoctorSchedules
                                               .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId && ds.Date.Date == selectedDate.Date);

            if (doctorSchedule == null)
            {
                doctorSchedule = new DoctorSchedule
                {
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    LunchStartTime = new TimeSpan(12, 0, 0),
                    LunchEndTime = new TimeSpan(13, 0, 0),
                };
            }

            var bookedAppointments = await _context.Appointments
                                                   .Where(a => a.DoctorId == doctorId
                                                            && a.AppointmentDateTime.Date == selectedDate.Date
                                                            && a.Status != "Cancelled"
                                                            && a.Status != "Completed")
                                                   .Select(a => a.AppointmentDateTime.TimeOfDay)
                                                   .ToListAsync();

            TimeSpan slotDuration = TimeSpan.FromMinutes(30);
            List<string> availableSlots = new List<string>();

            for (TimeSpan time = doctorSchedule.StartTime; time < doctorSchedule.EndTime; time = time.Add(slotDuration))
            {
                if ((time >= doctorSchedule.LunchStartTime && time < doctorSchedule.LunchEndTime) ||
                    (time.Add(slotDuration) > doctorSchedule.LunchStartTime && time.Add(slotDuration) <= doctorSchedule.LunchEndTime))
                {
                    continue;
                }

                if (!bookedAppointments.Contains(time))
                {
                    availableSlots.Add(new DateTime().Add(time).ToString(@"hh\:mm tt"));
                }
            }

            return Json(new { success = true, slots = availableSlots });
        }
    }
}
