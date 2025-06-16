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
using HospitalManagementSystem.Utility; // Import the PasswordHasher utility
using System.Text.Json; // Added for JsonSerializerOptions

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
        private int? GetDoctorIdFromSession() => HttpContext.Session.GetInt32("DoctorId");

        // GET: Doctor/Dashboard
        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Doctor")
            {
                TempData["ErrorMessage"] = "You must be logged in as a doctor to access the dashboard.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Doctor Dashboard";
            ViewBag.Username = username; // Display full username including prefix
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
                doctors = doctors.Where(d => d.Name.Contains(searchString) || d.Contact.Contains(searchString) || d.Username.Contains(searchString));
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
            return View(new Doctor()); // Pass an empty Doctor model
        }

        // POST: Doctor/Create (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor,
            string username,
            string password) // password is the plain text password from the form
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                return BadRequest(new { success = false, message = "Unauthorized: You are not authorized to add new doctors." });
            }

            ModelState.Clear(); // Clear any initial ModelState errors from default binding

            // --- Manual Validation and Assignment for Username and PasswordHash ---
            doctor.Username = username?.Trim();

            // Username Validation
            if (string.IsNullOrWhiteSpace(doctor.Username))
            {
                ModelState.AddModelError("Username", "Username is required.");
            }
            else if (doctor.Username.Length < 3 || doctor.Username.Length > 50)
            {
                ModelState.AddModelError("Username", "Username must be between 3 and 50 characters.");
            }

            // Password Validation and Hashing
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Password is required.");
            }
            else if (password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
            }
            else
            {
                doctor.PasswordHash = PasswordHasher.HashPassword(password);
            }

            // Check for duplicate username (for both Doctor and User entities)
            if (!string.IsNullOrEmpty(doctor.Username))
            {
                if (await _context.Doctors.AnyAsync(d => d.Username == doctor.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken by another doctor.");
                }
                // Also check in the general Users table if it's separate
                if (await _context.Users.AnyAsync(u => u.Username == doctor.Username))
                {
                    ModelState.AddModelError("Username", "This username is already in use by another user account.");
                }
            }
            // --- End Manual Validation and Assignment ---

            // Now re-evaluate ModelState.IsValid AFTER all manual validations and assignments
            if (!TryValidateModel(doctor) || !ModelState.IsValid)
            {
                var errors = new Dictionary<string, List<string>>();
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        errors[state.Key] = state.Value.Errors.Select(e => e.ErrorMessage).ToList();
                    }
                }
                Debug.WriteLine($"DoctorController.Create: Model state invalid. Errors: {JsonSerializer.Serialize(errors)}");
                return BadRequest(new { success = false, message = "Validation errors occurred.", errors = errors });
            }

            try
            {
                // Start a transaction to ensure atomicity
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    _context.Add(doctor); // Add the doctor first
                    await _context.SaveChangesAsync(); // Save to get the generated DoctorId

                    // Now create the associated User record
                    var newUser = new User
                    {
                        Username = doctor.Username,
                        PasswordHash = doctor.PasswordHash, // Use the hashed password
                        Role = "Doctor",
                        DoctorId = doctor.Id // Link to the newly created Doctor
                    };

                    _context.Add(newUser); // Add the user
                    await _context.SaveChangesAsync(); // Save the user

                    await transaction.CommitAsync(); // Commit the transaction

                    Debug.WriteLine($"Doctor {doctor.Name} and associated User '{newUser.Username}' added successfully!");
                    return Json(new { success = true, message = $"Doctor {doctor.Name} added successfully! Username: {doctor.Username}", redirectToUrl = Url.Action(nameof(Manage)) });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding doctor and user: {ex.Message}");
                return BadRequest(new { success = false, message = $"An error occurred while adding the doctor and user: {ex.Message}" });
            }
        }

        // GET: Doctor/Edit/5 (Admin Only for managing OTHER doctors)
        // Doctor's own edit is via MyInfo
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin()) // Only Admin can use this action
            {
                TempData["ErrorMessage"] = "You are not authorized to edit other doctor's details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            ViewData["Title"] = "Edit Doctor";
            return View(doctor);
        }

        // POST: Doctor/Edit/5 (Admin Only for managing OTHER doctors)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Specialization,Description,Contact,Location,Username")] Doctor doctor)
        {
            if (!IsLoggedIn() || !IsAdmin()) // Only Admin can use this action
            {
                TempData["ErrorMessage"] = "You are not authorized to edit other doctor's details.";
                return RedirectToAction("Login", "Account");
            }

            if (id != doctor.Id) return NotFound();

            // Get original doctor to handle concurrency and password hash (which is not changed via this form)
            var originalDoctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (originalDoctor == null)
            {
                TempData["ErrorMessage"] = "The doctor you are trying to edit was not found.";
                return NotFound();
            }

            // Check for duplicate username if changed by Admin
            if (originalDoctor.Username != doctor.Username && await _context.Doctors.AnyAsync(d => d.Username == doctor.Username && d.Id != doctor.Id))
            {
                ModelState.AddModelError("Username", "This username is already taken by another doctor.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve the original password hash as it's not handled in this Admin-edit form
                    doctor.PasswordHash = originalDoctor.PasswordHash;
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor details updated successfully!";
                    return RedirectToAction(nameof(Manage)); // Redirect to manage page after Admin edit
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
                    {
                        TempData["ErrorMessage"] = "The doctor record was deleted by another user. Cannot save changes.";
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating doctor: {ex.Message}");
                    TempData["ErrorMessage"] = $"An error occurred while updating the doctor: {ex.Message}";
                }
            }
            ViewData["Title"] = "Edit Doctor";
            return View(doctor);
        }


        // GET: Doctor/MyInfo (Doctor's own profile page for editing)
        [HttpGet]
        public async Task<IActionResult> MyInfo()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view your profile.";
                return RedirectToAction("Login", "Account");
            }

            int? doctorId = GetDoctorIdFromSession();
            if (!doctorId.HasValue || doctorId.Value == 0)
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Your doctor profile could not be found.";
                return RedirectToAction("Logout", "Account"); // Force logout if profile not found
            }

            // Do not pass PasswordHash to the view directly.
            // The NewPassword property on the model is used for input only.
            ViewData["Title"] = "My Profile";
            return View(doctor);
        }

        // POST: Doctor/EditMyInfo (Doctor edits their own profile)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyInfo(
            [Bind("Id,Name,Specialization,Description,Contact,Location,Username,NewPassword")] Doctor doctor)
        {
            Debug.WriteLine("EditMyInfo POST action called.");

            if (!IsLoggedIn() || !IsDoctor())
            {
                Debug.WriteLine("EditMyInfo: User not logged in or not a Doctor.");
                return BadRequest(new { success = false, message = "Unauthorized: You must be logged in as a Doctor to edit your profile." });
            }

            int? sessionDoctorId = GetDoctorIdFromSession();
            // Ensure the doctor is only editing their own profile
            if (!sessionDoctorId.HasValue || sessionDoctorId.Value == 0 || sessionDoctorId.Value != doctor.Id)
            {
                Debug.WriteLine($"EditMyInfo: Unauthorized attempt to edit another doctor's profile. Session ID: {sessionDoctorId}, Doctor ID from form: {doctor.Id}");
                return BadRequest(new { success = false, message = "Unauthorized attempt to edit another doctor's profile." });
            }

            // Fetch the original doctor from the database to compare changes and get existing password hash
            var originalDoctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctor.Id);
            if (originalDoctor == null)
            {
                Debug.WriteLine($"EditMyInfo: Original doctor (ID: {doctor.Id}) not found in DB.");
                return BadRequest(new { success = false, message = "Your doctor profile could not be found for update." });
            }

            // Clear ModelState to re-validate after manual assignments
            ModelState.Clear();

            // --- Manual Validation and Assignment ---
            // If the username is somehow changed in the UI, re-check for duplicates.
            // Note: The UI currently displays username as readonly. If you enable editing, this is crucial.
            if (originalDoctor.Username != doctor.Username)
            {
                if (await _context.Doctors.AnyAsync(d => d.Username == doctor.Username && d.Id != doctor.Id))
                {
                    ModelState.AddModelError("Username", "This username is already taken. Please choose a different one.");
                }
                // Also check in the general Users table if it's separate
                if (await _context.Users.AnyAsync(u => u.Username == doctor.Username && u.DoctorId != doctor.Id && u.Role == "Doctor")) // Ensure it's not the same user's existing entry
                {
                    ModelState.AddModelError("Username", "This username is already in use by another user account.");
                }
            }

            // Password handling: Only update if a new password is provided
            if (!string.IsNullOrEmpty(doctor.NewPassword))
            {
                if (doctor.NewPassword.Length < 6)
                {
                    ModelState.AddModelError("NewPassword", "New password must be at least 6 characters long.");
                }
                else
                {
                    doctor.PasswordHash = PasswordHasher.HashPassword(doctor.NewPassword); // Hash the new password
                }
            }
            else
            {
                // If NewPassword is empty, retain the existing PasswordHash from the original doctor
                doctor.PasswordHash = originalDoctor.PasswordHash;
            }

            // Manual re-validation of required fields (even if they have [Required] attributes,
            // sometimes explicit checks are needed for binding issues or clarity)
            if (string.IsNullOrWhiteSpace(doctor.Name)) ModelState.AddModelError("Name", "Doctor Name is required.");
            if (string.IsNullOrWhiteSpace(doctor.Specialization)) ModelState.AddModelError("Specialization", "Specialization is required.");
            if (string.IsNullOrWhiteSpace(doctor.Contact)) ModelState.AddModelError("Contact", "Contact information is required.");
            if (string.IsNullOrWhiteSpace(doctor.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (string.IsNullOrWhiteSpace(doctor.Username)) ModelState.AddModelError("Username", "Username is required.");
            // --- End Manual Validation and Assignment ---

            // Re-validate the model after manual assignments and additions to ModelState
            if (!TryValidateModel(doctor) || !ModelState.IsValid)
            {
                Debug.WriteLine("EditMyInfo: ModelState is invalid after manual checks.");
                var errors = new Dictionary<string, List<string>>();
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        errors[state.Key] = state.Value.Errors.Select(e => e.ErrorMessage).ToList();
                    }
                }
                Debug.WriteLine($"EditMyInfo ModelState Errors: {JsonSerializer.Serialize(errors)}");
                return BadRequest(new { success = false, message = "Validation errors occurred. Please check the form.", errors = errors });
            }

            try
            {
                // Attach the original doctor entity and update its properties
                // This ensures we're tracking an existing entity and only modifying allowed properties.
                // It's safer than _context.Update(doctor) if 'doctor' comes from binding
                // and might not have its full state (like navigation properties) attached.
                _context.Doctors.Attach(originalDoctor);
                _context.Entry(originalDoctor).CurrentValues.SetValues(doctor); // Update all scalar properties from 'doctor' to 'originalDoctor'

                // If password was changed, also update in the Users table
                if (!string.IsNullOrEmpty(doctor.NewPassword))
                {
                    var userAccount = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == originalDoctor.Id && u.Role == "Doctor");
                    if (userAccount != null)
                    {
                        userAccount.PasswordHash = doctor.PasswordHash; // Use the newly hashed password
                        _context.Users.Update(userAccount);
                        Debug.WriteLine($"EditMyInfo: Updated password for user '{userAccount.Username}'.");
                    }
                    else
                    {
                        Debug.WriteLine($"EditMyInfo: No associated User account found for doctor ID {originalDoctor.Id} to update password.");
                    }
                }
                // If the Doctor's name (which drives session username) was updated, update the session.
                // This logic should ideally be based on a property that changes, like doctor.Name or originalDoctor.Username
                // Assuming originalDoctor.Username is what's in session for the display username.
                // If the name (Doctor.Name) is what should reflect in session, use that.
                if (HttpContext.Session.GetString("Username") != doctor.Name) // Assuming session username stores doctor's display name
                {
                    HttpContext.Session.SetString("Username", doctor.Name);
                    Debug.WriteLine($"EditMyInfo: Session username updated to '{doctor.Name}'.");
                }


                await _context.SaveChangesAsync();
                Debug.WriteLine($"EditMyInfo: Doctor {doctor.Name} (ID: {doctor.Id}) updated successfully.");
                return Json(new { success = true, message = "Your profile has been updated successfully!", redirectToUrl = Url.Action(nameof(Dashboard)) }); // CHANGED THIS LINE
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Debug.WriteLine($"EditMyInfo: Concurrency error updating doctor (ID: {doctor.Id}): {ex.Message}");
                // Instead of RedirectToAction, return Json error for AJAX
                return BadRequest(new { success = false, message = "A concurrency error occurred. Your profile might have been updated by someone else. Please try again." });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating doctor profile (ID: {doctor.Id}): {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                // Instead of RedirectToAction, return Json error for AJAX
                return BadRequest(new { success = false, message = $"An unexpected error occurred while updating your profile: {ex.Message}. Please check server logs." });
            }
        }

        // GET: Doctor/Details/5 (Admin: any, Doctor: self, Patient: any)
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view doctor details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            // Authorization: Admin can see any doctor. Doctor can only see their own details via this action. Patient can see any doctor.
            bool isAuthorized = IsAdmin() ||
                                 (IsDoctor() && GetDoctorIdFromSession() == doctor.Id) ||
                                 IsPatient(); // Patients can view any doctor's public details

            if (!isAuthorized)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this doctor's details.";
                if (IsDoctor()) return RedirectToAction("MyInfo"); // Doctor redirect to their own info
                if (IsPatient()) return RedirectToAction("Search", "Doctor"); // Patient might go to a search page
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Doctor Details";
            return View(doctor);
        }

        // GET: Doctor/Delete/5 (Admin Only)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin()) // Only Admin can access Delete (GET)
            {
                TempData["ErrorMessage"] = "You are not authorized to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            ViewData["Title"] = "Delete Doctor";
            return View(doctor);
        }

        // POST: Doctor/Delete/5 (Admin Only)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin()) // Only Admin can perform Delete (POST)
            {
                return BadRequest(new { success = false, message = "Unauthorized to delete doctors." });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                    {
                        return BadRequest(new { success = false, message = "Doctor not found." });
                    }

                    // Check for associated appointments
                    var appointments = await _context.Appointments
                                                     .Where(a => a.DoctorId == doctor.Id)
                                                     .ToListAsync();
                    if (appointments.Any())
                    {
                        transaction.Rollback();
                        return BadRequest(new { success = false, message = $"Cannot delete doctor {doctor.Name}. There are {appointments.Count} existing appointments linked to this doctor. Please delete or reassign appointments first." });
                    }

                    // Delete associated schedules
                    var schedules = await _context.DoctorSchedules.Where(ds => ds.DoctorId == doctor.Id).ToListAsync();
                    if (schedules.Any())
                    {
                        _context.DoctorSchedules.RemoveRange(schedules);
                        await _context.SaveChangesAsync(); // Save changes for schedules within transaction
                        Debug.WriteLine($"Deleted {schedules.Count} schedules for doctor ID: {id}");
                    }

                    // Delete associated reviews
                    var reviews = await _context.DoctorReviews.Where(dr => dr.DoctorId == doctor.Id).ToListAsync();
                    if (reviews.Any())
                    {
                        _context.DoctorReviews.RemoveRange(reviews);
                        await _context.SaveChangesAsync(); // Save changes for reviews within transaction
                        Debug.WriteLine($"Deleted {reviews.Count} reviews for doctor ID: {id}");
                    }

                    // Delete associated user account
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == doctor.Id && u.Role == "Doctor");
                    if (user != null)
                    {
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync(); // Save changes for user within transaction
                        Debug.WriteLine($"Deleted associated user for doctor ID: {id}, Username: {user.Username}");
                    }
                    else
                    {
                        Debug.WriteLine($"No associated User record found for doctor ID: {id} or role is not Doctor.");
                    }

                    _context.Doctors.Remove(doctor);
                    await _context.SaveChangesAsync(); // Save changes for doctor within transaction

                    transaction.Commit();
                    return Json(new { success = true, message = $"Doctor {doctor.Name} and associated data deleted successfully!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"Error deleting doctor {id}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    return BadRequest(new { success = false, message = $"Error deleting doctor: {ex.Message}. Check server logs for details. This might be due to remaining related data." });
                }
            }
        }

        // GET: Doctor/Schedule (Doctor's Own Schedule with Date Filter)
        [HttpGet]
        public async Task<IActionResult> Schedule(DateTime? selectedDate) // Added nullable DateTime parameter
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a doctor to view your schedule.";
                return RedirectToAction("Login", "Account");
            }

            int? doctorId = GetDoctorIdFromSession();
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

            int? doctorId = GetDoctorIdFromSession();
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
