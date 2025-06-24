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
using System.Security.Cryptography; // Added for RandomNumberGenerator
using System.Text; // Added for StringBuilder
using Microsoft.EntityFrameworkCore.Storage; // Added for IDbContextTransaction.BeginTransactionAsync()
using Microsoft.Data.SqlClient; // ADDED THIS USING DIRECTIVE FOR SQLCLIENT


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

            // Allow both Doctor and Admin to access dashboard
            if (!IsLoggedIn() || (!IsDoctor() && !IsAdmin()))
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor or Admin to access the dashboard.";
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
            IQueryable<Doctor> doctors = _context.Doctors.Include(d => d.User); // Include User for search and display

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString) ||
                                             d.Contact.Contains(searchString) ||
                                             (d.User != null && d.User.Username.Contains(searchString))); // Search by username too
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
            [Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "Unauthorized: You are not authorized to add new doctors.";
                return RedirectToAction("Login", "Account");
            }

            // --- Manual Validation for Doctor's General Info (from Doctor Model) ---
            if (string.IsNullOrWhiteSpace(doctor.Name)) ModelState.AddModelError("Name", "Doctor Name is required.");
            if (string.IsNullOrWhiteSpace(doctor.Specialization)) ModelState.AddModelError("Specialization", "Specialization is required.");
            if (string.IsNullOrWhiteSpace(doctor.Contact)) ModelState.AddModelError("Contact", "Contact information is required.");
            if (string.IsNullOrWhiteSpace(doctor.Location)) ModelState.AddModelError("Location", "Location is required.");
            if (string.IsNullOrEmpty(doctor.Description)) doctor.Description = null; // Description is optional, allow null

            // NEW: Explicitly remove validation errors for User and NewPassword fields
            ModelState.Remove("User");
            ModelState.Remove("NewPassword");

            // Log all ModelState errors before checking IsValid
            Debug.WriteLine("--- ModelState Errors for Doctor/Create (POST) ---");
            foreach (var modelStateEntry in ModelState.Where(e => e.Value.Errors.Any()))
            {
                foreach (var error in modelStateEntry.Value.Errors)
                {
                    Debug.WriteLine($"  Key: {modelStateEntry.Key}, Error: {error.ErrorMessage}");
                }
            }
            Debug.WriteLine("--- End ModelState Errors ---");


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
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View(doctor);
            }

            // --- AUTO-GENERATION OF USERNAME ---
            string baseUsername = "doc." + doctor.Name.ToLower()
                                     .Replace(" ", "")
                                     .Replace(".", "")
                                     .Replace("-", "")
                                     .Trim();
            string generatedUsername = baseUsername;
            int counter = 1;
            int maxUsernameLength = 50;

            if (baseUsername.Length > maxUsernameLength - 5)
            {
                baseUsername = baseUsername.Substring(0, maxUsernameLength - 5);
            }

            generatedUsername = baseUsername;
            while (await _context.Users.AnyAsync(u => u.Username == generatedUsername))
            {
                generatedUsername = $"{baseUsername}_{counter}";
                if (generatedUsername.Length > maxUsernameLength)
                {
                    generatedUsername = $"{baseUsername.Substring(0, Math.Min(baseUsername.Length, maxUsernameLength - counter.ToString().Length - 1))}_{counter}";
                }
                counter++;
            }

            // --- Transaction for saving Doctor and User ---
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // 1. Save Doctor first to get its ID (if auto-generated)
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync(); // This will populate doctor.Id

                    // 2. The plain password is the generated username itself
                    string generatedPasswordPlain = generatedUsername;
                    string generatedPasswordHash = PasswordHasher.HashPassword(generatedPasswordPlain);

                    // 3. Create the User account with generated credentials
                    var newUser = new User
                    {
                        Username = generatedUsername,
                        PasswordHash = generatedPasswordHash,
                        Role = "Doctor",
                        CreatedAt = DateTime.Now,
                        LastLogin = DateTime.Now,
                        DoctorId = doctor.Id // Link the new User to the newly created doctor here
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 4. IMPORTANT: Link the Doctor back to the User's ID
                    // This ensures the Doctor's UserId navigation property correctly points to the User.
                    doctor.UserId = newUser.Id;
                    _context.Doctors.Update(doctor);
                    await _context.SaveChangesAsync(); // Save the Doctor's UserId now

                    await transaction.CommitAsync();

                    // Display the generated username and password (which is the username) in the success message
                    TempData["SuccessMessage"] = $"Doctor {doctor.Name} created successfully! Their Username is: <span class='fw-bold'>{newUser.Username}</span>";
                    Debug.WriteLine($"Doctor {doctor.Name} and associated User '{newUser.Username}' added successfully. Password was auto-generated as username. Doctor UserId: {doctor.UserId}. New User's DoctorId: {newUser.DoctorId}.");
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

        // Helper function to generate a unique username based on doctor's name
        private string GenerateUniqueUsername(string doctorName)
        {
            string baseUsername = "doc." + Regex.Replace(doctorName.ToLower(), @"[^a-z0-9]", "");
            if (baseUsername.Length > 45)
            {
                baseUsername = baseUsername.Substring(0, 45);
            }
            return baseUsername;
        }

        // Removed GenerateRandomPassword as it's no longer needed for new doctor creation
        // private string GenerateRandomPassword(int length = 12) { ... }


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
                return View(originalDoctor);
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

            Debug.WriteLine("ModelState is VALID for Admin Doctor Edit. Attempting to save changes...");
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
                Debug.WriteLine($"DbUpdateConcurrencyException for Doctor ID {doctor.Id}: {ex.Message}");
                TempData["ErrorMessage"] = "A concurrency error occurred. Your profile might have been updated by someone else. Please try again.";
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
                Debug.WriteLine($"General Exception during save for Doctor ID {doctor.Id}: {ex.Message}");
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
                TempData["ErrorMessage"] = "Unauthorized to delete doctors.";
                return RedirectToAction(nameof(Manage));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fetch the doctor along with ALL directly related entities that might prevent deletion
                    var doctor = await _context.Doctors
                                               .Include(d => d.User) // Still include, but we'll robustly check below
                                               .Include(d => d.Appointments)
                                               .Include(d => d.Schedules)
                                               .Include(d => d.Reviews)
                                               .Include(d => d.Bills)
                                               .FirstOrDefaultAsync(d => d.Id == id);

                    if (doctor == null)
                    {
                        TempData["ErrorMessage"] = "Doctor not found.";
                        return RedirectToAction(nameof(Manage));
                    }

                    Debug.WriteLine($"[DEBUG] Deleting Doctor ID: {id}. Doctor.UserId: {(doctor.UserId.HasValue ? doctor.UserId.Value.ToString() : "NULL")}. Doctor.User navigation property is {(doctor.User == null ? "NULL" : "NOT NULL")}.");


                    // --- CRITICAL BUSINESS LOGIC CHECK: Prevent deletion if doctor has ACTIVE appointments ---
                    if (doctor.Appointments != null && doctor.Appointments.Any(a => a.Status != "Completed" && a.Status != "Cancelled"))
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Cannot delete doctor '{doctor.Name}': There are pending or scheduled appointments linked to this doctor. Please cancel or complete them first.";
                        return RedirectToAction(nameof(Manage));
                    }

                    // --- Step-by-step deletion/disassociation of associated entities to satisfy FK constraints ---

                    // 1. Handle User (OnDelete(DeleteBehavior.Restrict) from Doctor to User)
                    // Robustly find the associated User using the FK on the User table, rather than relying solely on navigation property
                    User associatedUser = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == id);

                    if (associatedUser != null)
                    {
                        Debug.WriteLine($"[DEBUG] Found associated User: '{associatedUser.Username}' (ID: {associatedUser.Id}) for Doctor ID: {id}.");

                        // Sever the foreign key link on the User side
                        associatedUser.DoctorId = null;
                        _context.Users.Update(associatedUser); // Mark for update
                        await _context.SaveChangesAsync(); // Commit the update (nullification) *immediately* within the transaction
                        Debug.WriteLine($"[DEBUG] Successfully disassociated User '{associatedUser.Username}' from Doctor ID: {id}. Now attempting to remove User.");

                        _context.Users.Remove(associatedUser); // Mark user for deletion
                        await _context.SaveChangesAsync(); // Commit the user deletion *immediately* within the transaction
                        Debug.WriteLine($"[DEBUG] Successfully deleted User '{associatedUser.Username}'.");
                    }
                    else
                    {
                        Debug.WriteLine($"[DEBUG] No associated User record found with DoctorId = {id} in the Users table. Skipping explicit user disassociation and deletion.");
                    }


                    // 2. Handle Appointments (OnDelete(DeleteBehavior.Restrict) from Doctor to Appointment)
                    // Explicitly delete all appointments related to this doctor.
                    if (doctor.Appointments != null && doctor.Appointments.Any())
                    {
                        Debug.WriteLine($"[DEBUG] Attempting to delete {doctor.Appointments.Count} appointments for Doctor ID: {id}...");
                        _context.Appointments.RemoveRange(doctor.Appointments);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"[DEBUG] Successfully deleted {doctor.Appointments.Count} appointments.");
                    }

                    // 3. Handle Bills (OnDelete(DeleteBehavior.SetNull) from Doctor to Bill)
                    if (doctor.Bills != null && doctor.Bills.Any())
                    {
                        Debug.WriteLine($"[DEBUG] Attempting to delete {doctor.Bills.Count} bills for Doctor ID: {id}...");
                        _context.Bills.RemoveRange(doctor.Bills);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"[DEBUG] Successfully deleted {doctor.Bills.Count} bills.");
                    }

                    // 4. Handle DoctorSchedules (OnDelete(DeleteBehavior.Cascade))
                    if (doctor.Schedules != null && doctor.Schedules.Any())
                    {
                        Debug.WriteLine($"[DEBUG] Attempting to delete {doctor.Schedules.Count} schedules for Doctor ID: {id}...");
                        _context.DoctorSchedules.RemoveRange(doctor.Schedules);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"[DEBUG] Successfully deleted {doctor.Schedules.Count} schedules.");
                    }

                    // 5. Handle DoctorReviews (OnDelete(DeleteBehavior.Cascade))
                    if (doctor.Reviews != null && doctor.Reviews.Any())
                    {
                        Debug.WriteLine($"[DEBUG] Attempting to delete {doctor.Reviews.Count} reviews for Doctor ID: {id}...");
                        _context.DoctorReviews.RemoveRange(doctor.Reviews);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"[DEBUG] Successfully deleted {doctor.Reviews.Count} reviews.");
                    }

                    // 6. Finally, delete the doctor itself
                    Debug.WriteLine($"[DEBUG] Attempting to delete Doctor ID: {id}...");
                    _context.Doctors.Remove(doctor);
                    await _context.SaveChangesAsync();
                    Debug.WriteLine($"[DEBUG] Successfully deleted Doctor ID: {id}.");

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = $"Doctor '{doctor.Name}' and all associated data deleted successfully!";
                    return RedirectToAction(nameof(Manage));
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine($"[ERROR] DbUpdateException during doctor deletion (ID: {id}): {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Debug.WriteLine($"[ERROR] Inner Exception: {dbEx.InnerException.Message}");
                        if (dbEx.InnerException is SqlException sqlEx)
                        {
                            Debug.WriteLine($"[SQL ERROR] Number: {sqlEx.Number}, State: {sqlEx.State}, Class: {sqlEx.Class}");
                            Debug.WriteLine($"[SQL ERROR] Message: {sqlEx.Message}");
                            foreach (SqlError error in sqlEx.Errors)
                            {
                                Debug.WriteLine($"[SQL ERROR DETAILS] Source: {error.Source}, Message: {error.Message}, Line: {error.LineNumber}, Procedure: {error.Procedure}");
                            }
                        }
                    }
                    TempData["ErrorMessage"] = "Database error: Could not delete doctor due to remaining linked data. Please check **Visual Studio Output window** for detailed error logs.";
                    return RedirectToAction(nameof(Manage));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine($"[ERROR] General error during doctor deletion (ID: {id}): {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                    }
                    TempData["ErrorMessage"] = $"An unexpected error occurred during deletion: {ex.Message}. Please check **Visual Studio Output window** for detailed error logs.";
                    return RedirectToAction(nameof(Manage));
                }
            }
        }

        // THIS IS THE SINGLE, CORRECT DoctorExists METHOD
        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
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
    }
}
