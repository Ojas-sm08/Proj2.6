using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For Session extensions
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using System.Diagnostics; // For Debug.WriteLine
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

        // Helper methods for session-based role/ID checks
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsPatient() => HttpContext.Session.GetString("Role") == "Patient";

        private int? GetPatientIdFromSession() => HttpContext.Session.GetInt32("PatientId");
        private int? GetDoctorIdFromSession() => HttpContext.Session.GetInt32("DoctorId");


        // GET: Appointment/AllAppointments (Admin and Doctor View)
        [HttpGet]
        public async Task<IActionResult> AllAppointments(string searchString)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to view all appointments.";
                return RedirectToAction("Login", "Account");
            }

            IQueryable<Appointment> appointments = _context.Appointments
                                                            .Include(a => a.Patient)
                                                            .Include(a => a.Doctor);

            if (IsDoctor())
            {
                int? doctorId = GetDoctorIdFromSession();
                if (!doctorId.HasValue || doctorId.Value == 0)
                {
                    TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }
                appointments = appointments.Where(a => a.DoctorId == doctorId.Value);
                ViewData["Title"] = "My Appointments"; // Doctor's own appointments
            }
            else // IsAdmin()
            {
                ViewData["Title"] = "All Hospital Appointments"; // Admin sees all
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                appointments = appointments.Where(a =>
                    (a.Patient != null && a.Patient.Name != null && a.Patient.Name.Contains(searchString)) ||
                    (a.Doctor != null && a.Doctor.Name != null && a.Doctor.Name.Contains(searchString)) ||
                    (a.Reason != null && a.Reason.Contains(searchString)) ||
                    (a.Status != null && a.Status.Contains(searchString)));
            }
            ViewData["CurrentFilter"] = searchString; // Preserve search string in view

            return View(await appointments.OrderByDescending(a => a.AppointmentDateTime).ToListAsync());
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

            int? patientId = GetPatientIdFromSession();
            if (!patientId.HasValue || patientId.Value == 0)
            {
                TempData["ErrorMessage"] = "Patient ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var patientAppointments = await _context.Appointments
                                                    .Include(a => a.Doctor)
                                                    .Include(a => a.Patient)
                                                    .Where(a => a.PatientId == patientId.Value)
                                                    .OrderByDescending(a => a.AppointmentDateTime)
                                                    .ToListAsync();

            ViewData["Title"] = "My Appointments";
            return View(patientAppointments);
        }

        // GET: Appointment/Create (Allows pre-filling of patient/doctor IDs)
        [HttpGet]
        public async Task<IActionResult> Create(int? patientId, int? doctorId)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to book an appointment.";
                return RedirectToAction("Login", "Account");
            }

            if (IsPatient())
            {
                patientId = GetPatientIdFromSession();
            }

            ViewBag.Patients = await _context.Patients
                                             .OrderBy(p => p.Name)
                                             .Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" })
                                             .ToListAsync();
            ViewBag.Doctors = await _context.Doctors
                                            .OrderBy(d => d.Name)
                                            .Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" })
                                            .ToListAsync();

            var model = new Appointment
            {
                PatientId = patientId ?? 0,
                DoctorId = doctorId ?? 0,
                AppointmentDateTime = DateTime.Now.AddHours(1).Date.AddHours(9), // Suggests a default future date/time
                Status = "Scheduled"
            };

            ViewData["Title"] = "Book New Appointment";
            return View(model);
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,AppointmentDateTime,Reason,Location,Status")] Appointment appointment)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to book an appointment.";
                return RedirectToAction("Login", "Account");
            }

            if (IsPatient())
            {
                int? sessionPatientId = GetPatientIdFromSession();
                if (!sessionPatientId.HasValue || sessionPatientId.Value == 0 || sessionPatientId.Value != appointment.PatientId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to book appointments for other patients or your session is invalid.";
                    return RedirectToAction("Login", "Account");
                }
            }
            else if (IsDoctor())
            {
                int? sessionDoctorId = GetDoctorIdFromSession();
                if (!sessionDoctorId.HasValue || sessionDoctorId.Value == 0)
                {
                    TempData["ErrorMessage"] = "Your doctor session is invalid. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }
            }
            else if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Unauthorized access to appointment booking.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(appointment.Status))
                {
                    appointment.Status = "Scheduled";
                }

                if (appointment.AppointmentDateTime < DateTime.Now && appointment.Status == "Scheduled")
                {
                    ModelState.AddModelError("AppointmentDateTime", "Appointment date and time cannot be in the past for new appointments.");
                    ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                    ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                    ViewData["Title"] = "Book New Appointment";
                    return View(appointment);
                }

                bool isDoctorBusy = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == appointment.DoctorId &&
                    a.AppointmentDateTime == appointment.AppointmentDateTime &&
                    a.Status != "Cancelled" && a.Status != "Completed");

                if (isDoctorBusy)
                {
                    TempData["ErrorMessage"] = "The selected doctor is not available at this exact time. Please choose another time.";
                    ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                    ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                    ViewData["Title"] = "Book New Appointment";
                    return View(appointment);
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment booked successfully!";

                if (IsPatient())
                {
                    return RedirectToAction("Appointments", "Appointment");
                }
                else if (IsDoctor())
                {
                    return RedirectToAction("Schedule", "Doctor"); // Assuming a Doctor/Schedule action
                }
                else
                {
                    return RedirectToAction("AllAppointments", "Appointment");
                }
            }

            ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
            ViewData["Title"] = "Book New Appointment";
            return View(appointment);
        }

        // GET: Appointment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to edit appointments.";
                return RedirectToAction("Login", "Account");
            }

            // *** IMPORTANT: Restrict patients from accessing Edit directly ***
            if (IsPatient())
            {
                TempData["ErrorMessage"] = "Patients are not authorized to edit appointments. You can only view details.";
                return RedirectToAction("Appointments", "Appointment"); // Redirect patient to their appointment list
            }

            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                                            .Include(a => a.Patient)
                                            .Include(a => a.Doctor)
                                            .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = $"Appointment with ID {id} not found.";
                return NotFound();
            }

            // Allow Admin or Doctor (if it's their appointment) to edit
            if (IsAdmin() || (IsDoctor() && GetDoctorIdFromSession() == appointment.DoctorId))
            {
                ViewBag.Patients = await _context.Patients.Select(p => new { p.PatientId, p.Name }).OrderBy(p => p.Name).ToListAsync();
                ViewBag.Doctors = await _context.Doctors.Select(d => new { d.Id, d.Name, d.Specialization }).OrderBy(d => d.Name).ToListAsync();
                ViewData["Title"] = "Edit Appointment";
                return View(appointment);
            }
            else
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this appointment.";
                if (IsDoctor()) return RedirectToAction("Schedule", "Doctor"); // Assuming a Doctor/Schedule action
                return RedirectToAction("Login", "Account"); // Fallback for other unauthorized roles
            }
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,AppointmentDateTime,Reason,Location,Status")] Appointment appointment)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to edit appointments.";
                return RedirectToAction("Login", "Account");
            }

            // *** IMPORTANT: Restrict patients from accessing Edit directly for POST ***
            if (IsPatient())
            {
                TempData["ErrorMessage"] = "Patients are not authorized to edit appointments. You can only view details.";
                return RedirectToAction("Appointments", "Appointment");
            }

            if (id != appointment.Id) return NotFound();

            var existingAppointment = await _context.Appointments
                                                    .AsNoTracking() // Use AsNoTracking for fetching before update
                                                    .Include(a => a.Patient)
                                                    .Include(a => a.Doctor)
                                                    .FirstOrDefaultAsync(a => a.Id == id);

            if (existingAppointment == null)
            {
                TempData["ErrorMessage"] = "The appointment you are trying to edit was not found or has been deleted.";
                return NotFound();
            }

            // Authorization: Admin, or Doctor if it's their appointment
            if (!(IsAdmin() || (IsDoctor() && GetDoctorIdFromSession() == existingAppointment.DoctorId)))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this appointment.";
                if (IsDoctor()) return RedirectToAction("Schedule", "Doctor");
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                ViewData["Title"] = "Edit Appointment";
                return View(appointment);
            }

            if (appointment.AppointmentDateTime < DateTime.Now && appointment.Status != "Completed" && appointment.Status != "Cancelled")
            {
                ModelState.AddModelError("AppointmentDateTime", "Appointment date and time cannot be in the past for scheduled appointments.");
                ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                ViewData["Title"] = "Edit Appointment";
                return View(appointment);
            }

            try
            {
                bool isDoctorBusy = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == appointment.DoctorId &&
                    a.AppointmentDateTime == appointment.AppointmentDateTime &&
                    a.Id != appointment.Id && // Exclude the current appointment being edited
                    a.Status != "Cancelled" && a.Status != "Completed");

                if (isDoctorBusy)
                {
                    TempData["ErrorMessage"] = "The selected doctor is not available at this exact time. Please choose another time.";
                    ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                    ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                    ViewData["Title"] = "Edit Appointment";
                    return View(appointment);
                }

                _context.Update(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment updated successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(appointment.Id))
                {
                    TempData["ErrorMessage"] = "The appointment was deleted by another user. Cannot save changes.";
                    return NotFound();
                }
                else
                {
                    throw; // Re-throw if a real concurrency issue
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the appointment: {ex.Message}";
                Debug.WriteLine($"Error updating appointment: {ex.Message}");
                ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                ViewData["Title"] = "Edit Appointment";
                return View(appointment);
            }

            if (IsPatient())
            {
                return RedirectToAction("Appointments", "Appointment");
            }
            else if (IsDoctor())
            {
                return RedirectToAction("Schedule", "Doctor");
            }
            else // IsAdmin()
            {
                return RedirectToAction("AllAppointments", "Appointment");
            }
        }

        // GET: Appointment/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view appointment details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();

            // Authorization: Admin, or Patient (for their own), or Doctor (for their own)
            if (!(IsAdmin() || (IsPatient() && GetPatientIdFromSession() == appointment.PatientId) || (IsDoctor() && GetDoctorIdFromSession() == appointment.DoctorId)))
            {
                TempData["ErrorMessage"] = "You are not authorized to view this appointment's details.";
                if (IsPatient()) return RedirectToAction("Appointments", "Appointment");
                if (IsDoctor()) return RedirectToAction("Schedule", "Doctor");
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Appointment Details";
            return View(appointment);
        }

        // GET: Appointment/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor())) // Only Admin/Doctor can access Delete (GET)
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

            // Doctor can only delete their own appointments
            if (IsDoctor() && GetDoctorIdFromSession() != appointment.DoctorId)
            {
                TempData["ErrorMessage"] = "Doctors can only delete their own appointments.";
                return RedirectToAction("Schedule", "Doctor");
            }

            ViewData["Title"] = "Delete Appointment";
            return View(appointment);
        }

        // POST: Appointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor())) // Only Admin/Doctor can perform Delete (POST)
            {
                return Json(new { success = false, message = "Unauthorized to delete appointments." });
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found." });
                }

                // Doctor can only delete their own appointments
                if (IsDoctor() && GetDoctorIdFromSession() != appointment.DoctorId)
                {
                    return Json(new { success = false, message = "Doctors can only delete their own appointments." });
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Appointment deleted successfully!" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting appointment: {ex.Message}");
                return Json(new { success = false, message = $"Error deleting appointment: {ex.Message}" });
            }
        }

        // POST: Appointment/Cancel (Patient can cancel their own, Doctor/Admin can cancel)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!IsLoggedIn() || (!IsPatient() && !IsAdmin() && !IsDoctor())) // Any logged-in user can initiate a cancel.
            {
                return Json(new { success = false, message = "You are not authorized to cancel appointments." });
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found." });
            }

            // Authorization check for patients: can only cancel their own.
            if (IsPatient())
            {
                int? patientId = GetPatientIdFromSession();
                if (!patientId.HasValue || patientId.Value != appointment.PatientId)
                {
                    return Json(new { success = false, message = "You can only cancel your own appointments." });
                }
            }
            // Authorization check for doctors: can only cancel their own appointments.
            else if (IsDoctor())
            {
                int? doctorId = GetDoctorIdFromSession();
                if (!doctorId.HasValue || doctorId.Value != appointment.DoctorId)
                {
                    return Json(new { success = false, message = "Doctors can only cancel their own appointments." });
                }
            }

            if (appointment.Status == "Cancelled")
            {
                return Json(new { success = false, message = "Appointment is already cancelled." });
            }
            if (appointment.Status == "Completed")
            {
                return Json(new { success = false, message = "Completed appointments cannot be cancelled." });
            }

            appointment.Status = "Cancelled";
            try
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Appointment cancelled successfully." });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cancelling appointment: {ex.Message}");
                return Json(new { success = false, message = $"Error cancelling appointment: {ex.Message}" });
            }
        }


        // GET: Appointment/CheckAvailability (renders Views/Appointment/Feature2.cshtml)
        // This action might not be directly related to the "Create" form's slot selection,
        // but its logic for determining available slots is similar to GetAvailableSlots.
        // Keeping it as is, but noting its separate purpose.
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
                                        .Select(d => new Doctor { Id = d.Id, Name = d.Name ?? "Unnamed Doctor", Specialization = d.Specialization ?? "N/A" })
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
            Debug.WriteLine($"GetAvailableSlots called: DoctorId={doctorId}, Date={selectedDate:yyyy-MM-dd}");

            if (doctorId <= 0 || selectedDate == DateTime.MinValue)
            {
                Debug.WriteLine("Invalid doctor or date provided in GetAvailableSlots (backend check).");
                Response.StatusCode = 400; // Bad Request
                return Json(new { success = false, message = "Invalid doctor or date provided." });
            }

            try
            {
                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    Debug.WriteLine($"Doctor with ID {doctorId} not found (backend check).");
                    Response.StatusCode = 404; // Not Found
                    return Json(new { success = false, message = "Doctor not found." });
                }

                var doctorSchedule = await _context.DoctorSchedules
                                                   .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId && ds.Date.Date == selectedDate.Date);

                // Use default schedule if no specific one is found for the date
                if (doctorSchedule == null)
                {
                    Debug.WriteLine($"No specific schedule for Doctor {doctorId} on {selectedDate.Date:yyyy-MM-dd}. Using default (9 AM - 5 PM).");
                    doctorSchedule = new DoctorSchedule
                    {
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0), // 5 PM
                        LunchStartTime = new TimeSpan(12, 0, 0), // 12 PM
                        LunchEndTime = new TimeSpan(13, 0, 0),   // 1 PM
                    };
                }

                var bookedAppointments = await _context.Appointments
                                                        .Where(a => a.DoctorId == doctorId
                                                                 && a.AppointmentDateTime.Date == selectedDate.Date
                                                                 && a.Status != "Cancelled"
                                                                 && a.Status != "Completed") // Only consider active appointments as booked
                                                        .Select(a => a.AppointmentDateTime.TimeOfDay)
                                                        .ToListAsync();

                Debug.WriteLine($"Found {bookedAppointments.Count} booked slots for DoctorId={doctorId} on {selectedDate.Date:yyyy-MM-dd}");
                foreach (var booked in bookedAppointments)
                {
                    Debug.WriteLine($"- Booked: {booked}");
                }

                TimeSpan slotDuration = TimeSpan.FromMinutes(30);
                List<string> availableSlotsFormatted = new List<string>();

                for (TimeSpan time = doctorSchedule.StartTime; time < doctorSchedule.EndTime; time = time.Add(slotDuration))
                {
                    // Check for lunch break and ensure next slot isn't during lunch
                    if ((time >= doctorSchedule.LunchStartTime && time < doctorSchedule.LunchEndTime) ||
                        (time.Add(slotDuration) > doctorSchedule.LunchStartTime && time.Add(slotDuration) <= doctorSchedule.LunchEndTime))
                    {
                        Debug.WriteLine($"- Skipping lunch break slot: {time}");
                        continue;
                    }

                    // Only consider future slots if the date is today
                    if (selectedDate.Date == DateTime.Today && time <= DateTime.Now.TimeOfDay)
                    {
                        Debug.WriteLine($"- Skipping past slot for today: {time}");
                        continue;
                    }

                    if (!bookedAppointments.Contains(time))
                    {
                        availableSlotsFormatted.Add(time.ToString(@"hh\:mm")); // Format to HH:MM (e.g., "09:00")
                        Debug.WriteLine($"- Adding available slot: {time.ToString(@"hh\:mm")}");
                    }
                    else
                    {
                        Debug.WriteLine($"- Slot {time} is booked.");
                    }
                }

                // If no slots are available, return with a specific message
                if (!availableSlotsFormatted.Any())
                {
                    Debug.WriteLine("No available slots found for this doctor/date combination (backend result).");
                    return Json(new { success = true, slots = new List<string> { "No slots available" }, message = "No slots found for this date and doctor." });
                }
                else
                {
                    // Add a default "Select slot" at the beginning for the dropdown if there are slots
                    availableSlotsFormatted.Insert(0, "-- Select Time Slot --");
                    Debug.WriteLine($"Returning {availableSlotsFormatted.Count - 1} available slots (plus default option).");
                    return Json(new { success = true, slots = availableSlotsFormatted }); // Return the list of strings
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in GetAvailableSlots (backend catch block): {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Response.StatusCode = 500; // Indicate a server error
                return Json(new { success = false, message = $"Server error while fetching slots: {ex.Message}" }); // Return error object with success=false
            }
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}
