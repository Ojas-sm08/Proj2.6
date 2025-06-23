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
using System.Text.RegularExpressions; // Added for Regex.IsMatch

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
        private int? GetUserIdFromSession() => HttpContext.Session.GetInt32("UserId");


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
                // Search based on Doctor's Name or Contact. Username is now on User model.
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
            return View(new Doctor()); // Pass an empty Doctor model
        }

        // POST: Doctor/Create (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Removed 'username' and 'password' parameters. They will be generated internally.
        public async Task<IActionResult> Create(
            [Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                return BadRequest(new { success = false, message = "Unauthorized: You are not authorized to add new doctors." });
            }

            // Clear ModelState to ensure re-validation after manual checks
            ModelState.Clear();

            // --- Manual Validation for Doctor's General Info (from Doctor Model) ---
            if (string.IsNullOrWhiteSpace(doctor.Name)) ModelState.AddModelError("Name", "Doctor Name is required.");
            if (string.IsNullOrWhiteSpace(doctor.Specialization)) ModelState.AddModelError("Specialization", "Specialization is required.");
            if (string.IsNullOrWhiteSpace(doctor.Contact)) ModelState.AddModelError("Contact", "Contact information is required.");
            if (string.IsNullOrWhiteSpace(doctor.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (string.IsNullOrEmpty(doctor.Description)) doctor.Description = null; // Allow null or empty for optional Description

            // If initial doctor details fail validation, return early
            if (!ModelState.IsValid)
            {
                var errors = new Dictionary<string, List<string>>();
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        errors[state.Key] = state.Value.Errors.Select(e => e.ErrorMessage).ToList();
                    }
                }
                Debug.WriteLine($"DoctorController.Create: Model state invalid for Doctor details. Errors: {JsonSerializer.Serialize(errors)}");
                TempData["ErrorMessage"] = "Please correct the errors in the form."; // General error for UI
                return View(doctor); // Return the view with the Doctor model to re-display form and errors
            }

            // --- AUTO-GENERATION OF USERNAME AND PASSWORD ---
            string baseUsername = "doc." + doctor.Name.ToLower()
                                                        .Replace(" ", "")
                                                        .Replace(".", "")
                                                        .Replace("-", "")
                                                        .Trim();
            string generatedUsername = baseUsername;
            int counter = 1;
            int maxUsernameLength = 50; // Ensure this matches your User.Username max length

            // Ensure baseUsername fits within maxUsernameLength, leaving room for counter
            if (baseUsername.Length > maxUsernameLength - 5) // Leave room for "_999" suffix
            {
                baseUsername = baseUsername.Substring(0, maxUsernameLength - 5);
            }

            // Loop to ensure unique username
            generatedUsername = baseUsername;
            while (await _context.Users.AnyAsync(u => u.Username == generatedUsername))
            {
                generatedUsername = $"{baseUsername}_{counter}";
                // Re-check length if counter makes it too long
                if (generatedUsername.Length > maxUsernameLength)
                {
                    generatedUsername = $"{baseUsername.Substring(0, Math.Min(baseUsername.Length, maxUsernameLength - counter.ToString().Length - 1))}_{counter}";
                }
                counter++;
            }

            // Password generation: For simplicity, use a portion of the generated username or a default.
            // For production, you'd want a more robust random password generation.
            string generatedPasswordPlain = generatedUsername; // Using username as password, or a default strong one
            if (generatedPasswordPlain.Length < 6) // Ensure minimum password length for hashing
            {
                generatedPasswordPlain += "P@ss1"; // Append to meet minimum length
            }
            else if (generatedPasswordPlain.Length > 100) // Truncate if too long
            {
                generatedPasswordPlain = generatedPasswordPlain.Substring(0, 100);
            }

            string generatedPasswordHash = PasswordHasher.HashPassword(generatedPasswordPlain);
            Debug.WriteLine($"Generated Username: {generatedUsername}, Generated Password (plain - partially shown): {generatedPasswordPlain.Substring(0, Math.Min(generatedPasswordPlain.Length, 1))}***, Hashed Password: {generatedPasswordHash.Substring(0, 5)}..."); // Censor sensitive data in logs

            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // 1. Create the User account with generated credentials
                    var newUser = new User
                    {
                        Username = generatedUsername,
                        PasswordHash = generatedPasswordHash,
                        Role = "Doctor",
                        CreatedAt = DateTime.Now,
                        LastLogin = DateTime.Now
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync(); // Save user to get the generated User.Id

                    // 2. Link the new User to the Doctor
                    doctor.UserId = newUser.Id; // Set the foreign key
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync(); // Save doctor

                    // Also set the DoctorId on the User record for reverse navigation if needed
                    newUser.DoctorId = doctor.Id;
                    _context.Users.Update(newUser); // Update the user with the new DoctorId
                    await _context.SaveChangesAsync(); // Save the updated user

                    await transaction.CommitAsync();

                    // Display the generated username and password in the success message
                    TempData["SuccessMessage"] = $"Doctor {doctor.Name} created successfully! Their Username is: <span class='fw-bold'>{newUser.Username}</span> and Password is: <span class='fw-bold'>{generatedPasswordPlain}</span>. Please save these credentials.";
                    Debug.WriteLine($"Doctor {doctor.Name} and associated User '{newUser.Username}' added successfully!");
                    return RedirectToAction(nameof(Manage));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding doctor and user: {ex.Message}");
                TempData["ErrorMessage"] = $"An error occurred while adding the doctor and user: {ex.Message}";
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return View(doctor); // Return the view with the model to show errors
            }
        }

        // GET: Doctor/Edit/5 (Admin Only for managing OTHER doctors' general details)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
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

        // POST: Doctor/Edit/5 (Admin Only for managing OTHER doctors' general details)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            Debug.WriteLine($"[POST] Doctor/Edit (Admin) called for ID: {id}");
            Debug.WriteLine($"Incoming Doctor object: Name={doctor.Name}, Specialization={doctor.Specialization}, Contact={doctor.Contact}");

            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit other doctor's details.";
                Debug.WriteLine("Authorization failed for Admin Edit.");
                return RedirectToAction("Login", "Account");
            }

            if (id != doctor.Id)
            {
                Debug.WriteLine($"ID mismatch: Route ID={id}, Model ID={doctor.Id}");
                return NotFound();
            }

            var existingDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id);
            if (existingDoctor == null)
            {
                TempData["ErrorMessage"] = "The doctor you are trying to edit was not found.";
                Debug.WriteLine($"Doctor with ID {id} not found in DB.");
                return NotFound();
            }

            existingDoctor.Name = doctor.Name;
            existingDoctor.Specialization = doctor.Specialization;
            existingDoctor.Description = doctor.Description;
            existingDoctor.Contact = doctor.Contact;
            existingDoctor.Location = doctor.Location;

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(existingDoctor.Name)) ModelState.AddModelError("Name", "Doctor Name is required.");
            if (string.IsNullOrWhiteSpace(existingDoctor.Specialization)) ModelState.AddModelError("Specialization", "Specialization is required.");
            if (string.IsNullOrWhiteSpace(existingDoctor.Contact)) ModelState.AddModelError("Contact", "Contact is required.");
            if (string.IsNullOrWhiteSpace(existingDoctor.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (string.IsNullOrEmpty(existingDoctor.Description)) existingDoctor.Description = null;


            Debug.WriteLine("--- ModelState Errors (Admin Doctor Edit) Before Final Save Attempt ---");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Debug.WriteLine($"  ModelState Error - Key: {state.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }
            Debug.WriteLine("--- End ModelState Errors (Admin Doctor Edit) ---");


            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is INVALID for Admin Doctor Edit. Returning view with errors.");
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                ViewData["Title"] = "Edit Doctor";
                return View(doctor);
            }

            Debug.WriteLine("ModelState is VALID for Admin Doctor Edit. Attempting to save changes...");
            try
            {
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Changes saved successfully for Doctor ID: {id}");
                TempData["SuccessMessage"] = "Doctor details updated successfully!";
                return RedirectToAction(nameof(Manage));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Debug.WriteLine($"DbUpdateConcurrencyException for Doctor ID {id}: {ex.Message}");
                TempData["ErrorMessage"] = "The doctor record was updated by another user or deleted. Please refresh and try again.";
                if (!DoctorExists(doctor.Id))
                {
                    Debug.WriteLine("Doctor no longer exists in DB after concurrency exception.");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Exception during save for Doctor ID {id}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = $"An error occurred while updating the doctor: {ex.Message}";
                ViewData["Title"] = "Edit Doctor";
                return View(doctor);
            }
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

            var doctor = await _context.Doctors
                                       .Include(d => d.User)
                                       .FirstOrDefaultAsync(d => d.Id == doctorId.Value);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Your doctor profile could not be found.";
                return RedirectToAction("Logout", "Account");
            }

            ViewBag.Username = doctor.User?.Username;

            ViewData["Title"] = "My Profile";
            return View(doctor);
        }

        // POST: Doctor/EditMyInfo (Doctor edits their own profile, including login details)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyInfo(
            [Bind("Id,Name,Specialization,Description,Contact,Location,NewPassword")] Doctor doctor,
            string username)
        {
            Debug.WriteLine("EditMyInfo POST action called.");

            if (!IsLoggedIn() || !IsDoctor())
            {
                Debug.WriteLine("EditMyInfo: User not logged in or not a Doctor.");
                return BadRequest(new { success = false, message = "Unauthorized: You must be logged in as a Doctor to edit your profile." });
            }

            int? sessionDoctorId = GetDoctorIdFromSession();
            if (!sessionDoctorId.HasValue || sessionDoctorId.Value == 0 || sessionDoctorId.Value != doctor.Id)
            {
                Debug.WriteLine($"EditMyInfo: Unauthorized attempt to edit another doctor's profile. Session ID: {sessionDoctorId}, Doctor ID from form: {doctor.Id}");
                return BadRequest(new { success = false, message = "Unauthorized attempt to edit another doctor's profile." });
            }

            var originalDoctor = await _context.Doctors
                                               .Include(d => d.User)
                                               .FirstOrDefaultAsync(d => d.Id == doctor.Id);
            if (originalDoctor == null)
            {
                Debug.WriteLine($"EditMyInfo: Original doctor (ID: {doctor.Id}) not found in DB.");
                TempData["ErrorMessage"] = "Your doctor profile could not be found for update.";
                return View(doctor);
            }

            originalDoctor.Name = doctor.Name;
            originalDoctor.Specialization = doctor.Specialization;
            originalDoctor.Description = doctor.Description;
            originalDoctor.Contact = doctor.Contact;
            originalDoctor.Location = doctor.Location;

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(originalDoctor.Name)) ModelState.AddModelError("Name", "Doctor Name is required.");
            if (string.IsNullOrWhiteSpace(originalDoctor.Specialization)) ModelState.AddModelError("Specialization", "Specialization is required.");
            if (string.IsNullOrWhiteSpace(originalDoctor.Contact)) ModelState.AddModelError("Contact", "Contact information is required.");
            if (string.IsNullOrWhiteSpace(originalDoctor.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (string.IsNullOrEmpty(originalDoctor.Description)) originalDoctor.Description = null;

            if (originalDoctor.User != null)
            {
                var trimmedUsername = username?.Trim();
                if (string.IsNullOrWhiteSpace(trimmedUsername))
                {
                    ModelState.AddModelError("username", "Username is required.");
                }
                else if (trimmedUsername.Length < 3 || trimmedUsername.Length > 50)
                {
                    ModelState.AddModelError("username", "Username must be between 3 and 50 characters.");
                }
                else if (originalDoctor.User.Username != trimmedUsername)
                {
                    if (await _context.Users.AnyAsync(u => u.Username == trimmedUsername && u.Id != originalDoctor.User.Id))
                    {
                        ModelState.AddModelError("username", "This username is already taken. Please choose another.");
                    }
                    else
                    {
                        originalDoctor.User.Username = trimmedUsername;
                    }
                }

                if (!string.IsNullOrEmpty(doctor.NewPassword))
                {
                    if (doctor.NewPassword.Length < 6)
                    {
                        ModelState.AddModelError("NewPassword", "New password must be at least 6 characters long.");
                    }
                    else
                    {
                        originalDoctor.User.PasswordHash = PasswordHasher.HashPassword(doctor.NewPassword);
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Associated user account not found for this doctor. Cannot update login details.");
                Debug.WriteLine($"EditMyInfo: No associated User account found for doctor ID {doctor.Id}. This is an unusual state.");
            }

            Debug.WriteLine("--- ModelState Errors (Doctor MyInfo) Before Final Save Attempt ---");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Debug.WriteLine($"  ModelState Error - Key: {state.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }
            Debug.WriteLine("--- End ModelState Errors (Doctor MyInfo) ---");

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("EditMyInfo: ModelState is INVALID. Returning view with errors.");
                ViewBag.Username = username;
                TempData["ErrorMessage"] = "Validation errors occurred. Please check the form.";
                return View(originalDoctor);
            }

            Debug.WriteLine("EditMyInfo: ModelState is VALID. Attempting to save changes...");
            try
            {
                await _context.SaveChangesAsync();
                Debug.WriteLine($"EditMyInfo: Doctor {originalDoctor.Name} (ID: {originalDoctor.Id}) and associated User updated successfully.");

                if (HttpContext.Session.GetString("Username") != originalDoctor.User?.Username)
                {
                    HttpContext.Session.SetString("Username", originalDoctor.User?.Username);
                    Debug.WriteLine($"EditMyInfo: Session username updated to '{originalDoctor.User?.Username}'.");
                }

                TempData["SuccessMessage"] = "Your profile has been updated successfully!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Debug.WriteLine($"EditMyInfo: Concurrency error updating doctor (ID: {doctor.Id}): {ex.Message}");
                TempData["ErrorMessage"] = "A concurrency error occurred. Your profile might have been updated by someone else. Please try again.";
                ViewBag.Username = username;
                return View(originalDoctor);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating doctor profile (ID: {doctor.Id}): {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = $"An unexpected error occurred while updating your profile: {ex.Message}. Please check server logs.";
                ViewBag.Username = username;
                return View(originalDoctor);
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

            bool isAuthorized = IsAdmin() ||
                               (IsDoctor() && GetDoctorIdFromSession() == doctor.Id) ||
                               IsPatient();

            if (!isAuthorized)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this doctor's details.";
                if (IsDoctor()) return RedirectToAction("MyInfo");
                if (IsPatient()) return RedirectToAction("Search", "Doctor");
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Doctor Details";
            return View(doctor);
        }

        // GET: Doctor/Delete/5 (Admin Only)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
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
            if (!IsLoggedIn() || !IsAdmin())
            {
                return BadRequest(new { success = false, message = "Unauthorized to delete doctors." });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var doctor = await _context.Doctors
                                               .Include(d => d.User)
                                               .FirstOrDefaultAsync(d => d.Id == id);
                    if (doctor == null)
                    {
                        return BadRequest(new { success = false, message = "Doctor not found." });
                    }

                    var appointments = await _context.Appointments
                                                     .Where(a => a.DoctorId == doctor.Id)
                                                     .ToListAsync();
                    if (appointments.Any())
                    {
                        transaction.Rollback();
                        return BadRequest(new { success = false, message = $"Cannot delete doctor {doctor.Name}. There are {appointments.Count} existing appointments linked to this doctor. Please delete or reassign appointments first." });
                    }

                    var schedules = await _context.DoctorSchedules.Where(ds => ds.DoctorId == doctor.Id).ToListAsync();
                    if (schedules.Any())
                    {
                        _context.DoctorSchedules.RemoveRange(schedules);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted {schedules.Count} schedules for doctor ID: {id}");
                    }

                    var reviews = await _context.DoctorReviews.Where(dr => dr.DoctorId == doctor.Id).ToListAsync();
                    if (reviews.Any())
                    {
                        _context.DoctorReviews.RemoveRange(reviews);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted {reviews.Count} reviews for doctor ID: {id}");
                    }

                    if (doctor.User != null)
                    {
                        _context.Users.Remove(doctor.User);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted associated user '{doctor.User.Username}' for doctor ID: {id}.");
                    }
                    else
                    {
                        Debug.WriteLine($"No associated User record found for doctor ID: {id} or User navigation property is null.");
                    }

                    _context.Doctors.Remove(doctor);
                    await _context.SaveChangesAsync();

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
        public async Task<IActionResult> Schedule(DateTime? selectedDate)
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

            DateTime dateToView = selectedDate?.Date ?? DateTime.Today.Date;

            var doctorSchedule = await _context.DoctorSchedules
                                                     .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId.Value && ds.Date.Date == dateToView.Date);

            var appointmentsForDate = await _context.Appointments
                                                     .Include(a => a.Patient)
                                                     .Where(a => a.DoctorId == doctorId.Value && a.AppointmentDateTime.Date == dateToView.Date)
                                                     .OrderBy(a => a.AppointmentDateTime)
                                                     .ToListAsync();

            ViewData["Title"] = "My Schedule";
            ViewData["CurrentSelectedDate"] = dateToView.ToString("yyyy-MM-dd");

            var viewModel = new DoctorScheduleViewModel
            {
                Doctor = doctor,
                DoctorSchedule = doctorSchedule,
                TodaysAppointments = appointmentsForDate,
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
