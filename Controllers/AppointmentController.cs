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
using System.Globalization; // For CultureInfo.InvariantCulture

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
                Status = "Scheduled",
                Price = 0.00m // Initialize price for the form
            };

            ViewData["Title"] = "Book New Appointment";
            return View(model);
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,AppointmentDateTime,Reason,Location,Status,Price")] Appointment appointment)
        {
            // Debugging the incoming appointment price
            Debug.WriteLine($"Attempting to create appointment with Price: {appointment.Price}");

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

            // Manually add price validation if not handled by default model binding attributes
            if (appointment.Price < 0)
            {
                ModelState.AddModelError("Price", "Price cannot be negative.");
            }
            // For now, let's set a default price if it's 0 or not set, as per user's request for automatic billing.
            // In a real app, this should come from user input or a price lookup.
            if (appointment.Price == 0.00m)
            {
                // This is a placeholder. You should replace this with actual pricing logic
                // e.g., fetching a price based on doctor, service, or pre-defined rates.
                appointment.Price = 100.00m; // Example: Set a default price of 100.00
                Debug.WriteLine($"Appointment price was 0 or not set, defaulting to: {appointment.Price}");
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

                // --- Begin: Automatic Bill Creation Logic ---
                // Add the appointment first to get its ID, which is needed for the bill
                _context.Add(appointment);
                await _context.SaveChangesAsync(); // Save appointment to get its ID

                Debug.WriteLine($"Appointment {appointment.Id} booked successfully for Patient {appointment.PatientId} with Doctor {appointment.DoctorId}. Price: {appointment.Price}");

                // Create a new Bill
                var newBill = new Bill
                {
                    AppointmentId = appointment.Id, // Link to the newly created appointment
                    PatientId = appointment.PatientId,
                    DoctorId = appointment.DoctorId,
                    BillDate = DateTime.Today,
                    Status = "Pending", // Initial status for a new bill
                    TotalAmount = appointment.Price, // Initial total amount from appointment price
                    Notes = $"Bill for Appointment #{appointment.Id} - {appointment.Reason}"
                };

                // Create a BillItem for the appointment fee
                var appointmentFeeItem = new BillItem
                {
                    ItemName = "Appointment Fee", // Or "Consultation Fee"
                    Quantity = 1,
                    UnitPrice = appointment.Price,
                    Amount = appointment.Price,
                    Bill = newBill // Link the bill item to the new bill
                };

                newBill.BillItems = new List<BillItem> { appointmentFeeItem }; // Add the item to the bill's collection

                _context.Bills.Add(newBill);
                await _context.SaveChangesAsync(); // Save the bill and its item

                Debug.WriteLine($"Bill {newBill.BillId} created for Appointment {appointment.Id} with Total Amount: {newBill.TotalAmount}");
                // --- End: Automatic Bill Creation Logic ---


                TempData["SuccessMessage"] = "Appointment booked and bill generated successfully!";

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

            // If ModelState is not valid, re-populate ViewBags and return to view
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

            // Always populate ViewBags for dropdowns regardless of authorization paths
            ViewBag.Patients = await _context.Patients
                                             .OrderBy(p => p.Name)
                                             .Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" })
                                             .ToListAsync();
            ViewBag.Doctors = await _context.Doctors
                                            .OrderBy(d => d.Name)
                                            .Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" })
                                            .ToListAsync();

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,AppointmentDateTime,Reason,Location,Status,Price")] Appointment appointment)
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

            // Authorization: Admin, or Doctor (if it's their appointment)
            if (!(IsAdmin() || (IsDoctor() && GetDoctorIdFromSession() == existingAppointment.DoctorId)))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this appointment.";
                if (IsDoctor()) return RedirectToAction("Schedule", "Doctor");
                return RedirectToAction("Login", "Account");
            }

            // Manually add price validation
            if (appointment.Price < 0)
            {
                ModelState.AddModelError("Price", "Price cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                // Re-populate ViewBags on ModelState invalid for POST
                ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name ?? "Unnamed Patient" }).ToListAsync();
                ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name ?? "Unnamed Doctor" }).ToListAsync();
                ViewData["Title"] = "Edit Appointment";
                return View(appointment);
            }

            if (appointment.AppointmentDateTime < DateTime.Now && appointment.Status != "Completed" && appointment.Status != "Cancelled")
            {
                ModelState.AddModelError("AppointmentDateTime", "Appointment date and time cannot be in the past for scheduled appointments.");
                // Re-populate ViewBags on ModelState invalid for POST
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
                    // Re-populate ViewBags on error for POST
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
                // Re-populate ViewBags on error for POST
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
                var appointment = await _context.Appointments
                                                .Include(a => a.Bills) // Include bills to handle cascading deletion
                                                .FirstOrDefaultAsync(a => a.Id == id);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found." });
                }

                // Doctor can only delete their own appointments
                if (IsDoctor() && GetDoctorIdFromSession() != appointment.DoctorId)
                {
                    return Json(new { success = false, message = "Doctors can only delete their own appointments." });
                }

                // Before deleting the appointment, ensure associated bills are handled.
                // Option 1: Delete associated bills and their items (Cascade Delete)
                if (appointment.Bills != null && appointment.Bills.Any())
                {
                    foreach (var bill in appointment.Bills.ToList()) // ToList() to avoid modification during enumeration
                    {
                        if (bill.BillItems != null && bill.BillItems.Any())
                        {
                            _context.BillItems.RemoveRange(bill.BillItems);
                        }
                        _context.Bills.Remove(bill);
                    }
                }
                // Option 2: Set AppointmentId in associated bills to null (if AppointmentId in Bill model is nullable)
                // var associatedBills = await _context.Bills.Where(b => b.AppointmentId == id).ToListAsync();
                // foreach (var bill in associatedBills)
                // {
                //      bill.AppointmentId = null; // Requires AppointmentId to be nullable in Bill model
                //      _context.Entry(bill).State = EntityState.Modified;
                // }


                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment deleted successfully!";
                return Json(new { success = true, message = "Appointment deleted successfully!" });
            }
            catch (DbUpdateException dbEx)
            {
                Debug.WriteLine($"DbUpdateException while deleting appointment {id}: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception (DbUpdateException): {dbEx.InnerException.Message}");
                }
                return Json(new { success = false, message = $"Database error: The appointment could not be deleted due to related records. Please ensure any associated bills are deleted first." });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting appointment: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = $"Error deleting appointment: {ex.Message}" });
            }
        }

        // GET: Appointment/CheckAvailability
        // This action renders a view to check doctor availability (which is your Feature2.cshtml)
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

                    viewModel.DoctorSchedule = doctorSchedule; // Assign the resolved doctorSchedule to the ViewModel

                    var bookedAppointments = await _context.Appointments
                                                            .Where(a => a.DoctorId == selectedDoctorId.Value
                                                                   && a.AppointmentDateTime.Date == viewModel.SelectedDate.Date
                                                                   && a.Status != "Cancelled"
                                                                   && a.Status != "Completed")
                                                            .Select(a => a.AppointmentDateTime.TimeOfDay)
                                                            .ToListAsync();

                    TimeSpan slotDuration = TimeSpan.FromMinutes(30);

                    // Generate available time slots based on the doctor's schedule
                    List<TimeSpan> availableSlots = new List<TimeSpan>();
                    for (TimeSpan time = doctorSchedule.StartTime; time < doctorSchedule.EndTime; time = time.Add(slotDuration))
                    {
                        // Skip lunch break
                        if (time >= doctorSchedule.LunchStartTime && time < doctorSchedule.LunchEndTime)
                        {
                            continue;
                        }

                        // Check if the slot is already booked
                        if (!bookedAppointments.Contains(time))
                        {
                            // Ensure the slot is not in the past
                            DateTime currentSlotDateTime = viewModel.SelectedDate.Date.Add(time);
                            if (currentSlotDateTime > DateTime.Now)
                            {
                                availableSlots.Add(time);
                            }
                        }
                    }
                    viewModel.AvailableSlots = availableSlots;
                }
                else
                {
                    TempData["ErrorMessage"] = "Selected doctor not found.";
                }
            }

            // Explicitly return the "Feature2" view with the populated viewModel
            return View("Feature2", viewModel);
        }

        // NEW ACTION: GetAvailableSlots - for AJAX calls from Book Appointment page
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, DateTime selectedDate)
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "Not authenticated." });
            }

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor not found." });
            }

            var doctorSchedule = await _context.DoctorSchedules
                                                     .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId && ds.Date.Date == selectedDate.Date);

            // Use default schedule if a specific one isn't found for the date
            if (doctorSchedule == null)
            {
                doctorSchedule = new DoctorSchedule
                {
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    LunchStartTime = new TimeSpan(12, 0, 0),
                    LunchEndTime = new TimeSpan(13, 0, 0),
                    DoctorId = doctorId,
                    Date = selectedDate.Date,
                    Location = "General Clinic"
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

            // Add a default option for the dropdown
            availableSlots.Add("-- Select Time Slot --");

            for (TimeSpan time = doctorSchedule.StartTime; time < doctorSchedule.EndTime; time = time.Add(slotDuration))
            {
                // Skip lunch break
                if (time >= doctorSchedule.LunchStartTime && time < doctorSchedule.LunchEndTime)
                {
                    continue;
                }

                // Check if the slot is already booked and is not in the past
                DateTime currentSlotDateTime = selectedDate.Date.Add(time);
                if (!bookedAppointments.Contains(time) && currentSlotDateTime > DateTime.Now)
                {
                    availableSlots.Add(currentSlotDateTime.ToString(@"hh\:mm tt", CultureInfo.InvariantCulture));
                }
            }

            if (!availableSlots.Any(s => s != "-- Select Time Slot --"))
            {
                // If only the default option exists, means no real slots are available
                return Json(new { success = true, message = "No slots available for this date and doctor.", slots = new List<string> { "No slots available" } });
            }

            return Json(new { success = true, message = "Slots loaded successfully.", slots = availableSlots });
        }


        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // NEW ACTION: Mark Appointment as Completed
        [HttpPost]
        [ValidateAntiForgeryToken] // Important for security with AJAX POST requests
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            // 1. Authorization Check: Only logged-in doctors should be able to mark appointments complete
            if (!IsLoggedIn() || !IsDoctor())
            {
                // Return a Forbidden status if unauthorized
                return Json(new { success = false, message = "You are not authorized to perform this action." });
            }

            // 2. Retrieve Appointment from DB
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found." });
            }

            // 3. Verify Doctor's Ownership/Association: Ensure the doctor marking it complete is the assigned doctor
            int? doctorIdFromSession = GetDoctorIdFromSession();
            if (!doctorIdFromSession.HasValue || appointment.DoctorId != doctorIdFromSession.Value)
            {
                // Log unauthorized access attempt if needed
                return Json(new { success = false, message = "You can only mark your own appointments as completed." });
            }

            // 4. Update Status (and potentially other relevant fields like ActualCompletionTime)
            if (appointment.Status == "Scheduled") // Only mark 'Scheduled' appointments as completed
            {
                appointment.Status = "Completed";
                // Optionally, add a completion timestamp
                // appointment.ActualCompletionTime = DateTime.Now; 
                _context.Update(appointment);

                try
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Appointment marked as completed successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency conflicts if multiple users try to update the same appointment
                    return Json(new { success = false, message = "Concurrency error: The appointment was modified or deleted by another user. Please refresh and try again." });
                }
                catch (Exception ex)
                {
                    // Log the full exception for debugging
                    Console.WriteLine($"Error marking appointment {id} as completed: {ex.Message}");
                    return Json(new { success = false, message = "An error occurred while saving changes." });
                }
            }
            else
            {
                return Json(new { success = false, message = $"Appointment status is already '{appointment.Status}'. Cannot mark as completed." });
            }
        }
    }
}
