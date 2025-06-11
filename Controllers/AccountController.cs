using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using System.Diagnostics;
using System;

namespace HospitalManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly HospitalDbContext _context;

        public AccountController(HospitalDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login or /Account/Login?role=Admin (or Doctor, Patient)
        [HttpGet]
        public IActionResult Login(string role = null)
        {
            ViewData["Title"] = "Login";

            if (!string.IsNullOrEmpty(role))
            {
                if (role != "Admin" && role != "Doctor" && role != "Patient")
                {
                    TempData["ErrorMessage"] = "Invalid role selected. Please choose Admin, Doctor, or Patient.";
                    role = null;
                }
                ViewData["ExpectedRole"] = role;
                ViewData["Title"] = $"{role} Login";
            }
            else
            {
                ViewData["ExpectedRole"] = null;
            }

            // If already logged in, redirect to respective dashboards
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                var sessionRole = HttpContext.Session.GetString("Role");
                if (sessionRole == "Admin")
                    return RedirectToAction("Index", "Admin"); // Corrected: Admin goes to Admin/Index
                else if (sessionRole == "Doctor")
                    return RedirectToAction("Dashboard", "Doctor");
                else if (sessionRole == "Patient")
                    return RedirectToAction("Dashboard", "Patient");
            }

            // Clear session if user is trying to log in again but session state is inconsistent
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                HttpContext.Session.Clear();
            }

            Debug.WriteLine($"AccountController.Login (GET): Showing login view. Expected Role: {role ?? "None"}");
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string expectedRole)
        {
            ViewData["ExpectedRole"] = expectedRole;
            ViewData["Title"] = $"{expectedRole} Login";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Please enter both username and password.";
                Debug.WriteLine("AccountController.Login (POST): Missing username or password.");
                return View();
            }

            // Include Patient and Doctor navigation properties if available on User model
            // This ensures PatientId/DoctorId are loaded directly with the user
            var user = await _context.Users
                                    .Include(u => u.Patient) // Assuming navigation property exists
                                    .Include(u => u.Doctor)  // Assuming navigation property exists
                                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid username. Please try again.";
                Debug.WriteLine($"AccountController.Login (POST): Invalid username '{username}'.");
                return View();
            }

            // REMINDER: In a real application, you MUST hash and verify passwords securely.
            // Do NOT store plain text passwords or compare them directly like this.
            if (user.PasswordHash != password)
            {
                TempData["ErrorMessage"] = "Invalid password. Please enter a valid password.";
                Debug.WriteLine($"AccountController.Login (POST): Invalid password for user '{username}'.");
                return View();
            }

            if (!string.IsNullOrEmpty(expectedRole) && user.Role != expectedRole)
            {
                TempData["ErrorMessage"] = $"Access Denied: You are trying to log in as {expectedRole}, but your account is registered as {user.Role}.";
                Debug.WriteLine($"AccountController.Login (POST): Role mismatch! User '{username}' is '{user.Role}' but tried to login as '{expectedRole}'.");
                return View();
            }

            // Store session variables
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            // Set specific IDs based on role using the directly loaded user properties
            if (user.Role == "Patient")
            {
                if (user.PatientId.HasValue && user.PatientId.Value > 0)
                {
                    HttpContext.Session.SetInt32("PatientId", user.PatientId.Value);
                    Debug.WriteLine($"AccountController.Login (POST): Patient '{user.Username}' logged in. PatientId: {user.PatientId.Value} set in session.");
                }
                else
                {
                    // This scenario suggests a user with "Patient" role but no linked PatientId or invalid ID
                    HttpContext.Session.SetInt32("PatientId", 0); // Set to 0 or null if you prefer
                    TempData["ErrorMessage"] += " Could not find matching patient profile for this user. Please contact support.";
                    Debug.WriteLine($"AccountController.Login (POST): User '{user.Username}' has Patient role but no valid PatientId. PatientId set to 0.");
                }
                HttpContext.Session.Remove("DoctorId"); // Clear DoctorId if patient logs in
            }
            else if (user.Role == "Doctor")
            {
                if (user.DoctorId.HasValue && user.DoctorId.Value > 0)
                {
                    HttpContext.Session.SetInt32("DoctorId", user.DoctorId.Value);
                    Debug.WriteLine($"AccountController.Login (POST): Doctor '{user.Username}' logged in. DoctorId: {user.DoctorId.Value} set in session.");
                }
                else
                {
                    // This scenario suggests a user with "Doctor" role but no linked DoctorId or invalid ID
                    HttpContext.Session.SetInt32("DoctorId", 0); // Set to 0 or null
                    TempData["ErrorMessage"] += " Could not find matching doctor profile for this user. Please contact support.";
                    Debug.WriteLine($"AccountController.Login (POST): User '{user.Username}' has Doctor role but no valid DoctorId. DoctorId set to 0.");
                }
                HttpContext.Session.Remove("PatientId"); // Clear PatientId if doctor logs in
            }
            else // Admin or other roles
            {
                // For admin, no PatientId or DoctorId is typically needed in session
                HttpContext.Session.Remove("PatientId");
                HttpContext.Session.Remove("DoctorId");
                Debug.WriteLine($"AccountController.Login (POST): Admin/Other role '{user.Username}' logged in. PatientId/DoctorId removed from session.");
            }

            TempData["SuccessMessage"] = $"Welcome, {user.Username}!";
            Debug.WriteLine($"AccountController.Login (POST): Successful login for '{user.Username}' with role '{user.Role}'.");

            // Redirect based on role
            if (user.Role == "Admin")
            {
                Debug.WriteLine("AccountController.Login (POST): Redirecting to Admin/Index for Admin.");
                return RedirectToAction("Index", "Admin"); // FIX: Corrected redirect for Admin
            }
            else if (user.Role == "Patient")
            {
                Debug.WriteLine("AccountController.Login (POST): Redirecting to Patient/Dashboard.");
                return RedirectToAction("Dashboard", "Patient");
            }
            else if (user.Role == "Doctor")
            {
                Debug.WriteLine("AccountController.Login (POST): Redirecting to Doctor/Dashboard.");
                return RedirectToAction("Dashboard", "Doctor");
            }
            else
            {
                Debug.WriteLine("AccountController.Login (POST): Unhandled role, redirecting to Home/Index.");
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/RegisterPatient
        [HttpGet]
        public IActionResult RegisterPatient()
        {
            ViewData["Title"] = "Patient Sign Up";
            return View();
        }

        // POST: /Account/RegisterPatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatient(PatientSignUpViewModel model)
        {
            ViewData["Title"] = "Patient Sign Up";
            if (ModelState.IsValid)
            {
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == model.Email || p.ContactNumber == model.ContactNumber);

                if (existingPatient != null)
                {
                    TempData["ErrorMessage"] = "A patient with this email or contact number already exists.";
                    return View(model);
                }

                var newPatient = new Patient
                {
                    Name = model.Name,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    ContactNumber = model.ContactNumber,
                    Email = model.Email,
                    Address = model.Address,
                    MedicalHistory = model.MedicalHistory
                };

                _context.Patients.Add(newPatient);
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Patient created: {newPatient.Name}, ID: {newPatient.PatientId}");

                string baseUsername = "pat." + model.Name.ToLower()
                                                       .Replace(" ", "")
                                                       .Replace(".", "");
                string generatedUsername = baseUsername;
                int counter = 1;
                while (await _context.Users.AnyAsync(u => u.Username == generatedUsername))
                {
                    generatedUsername = $"{baseUsername}{counter}";
                    counter++;
                }

                string generatedPassword = generatedUsername; // Set password same as username - REMINDER: HASH THIS!
                Debug.WriteLine($"Generated Username: {generatedUsername}, Password: {generatedPassword}");

                var newUser = new User
                {
                    Username = generatedUsername,
                    PasswordHash = generatedPassword, // REMINDER: Hash this in a real app!
                    Role = "Patient",
                    PatientId = newPatient.PatientId // Link the user to the newly created patient
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                Debug.WriteLine($"User created: {newUser.Username}, Linked PatientId: {newUser.PatientId}");

                // After successful registration, log the new user in
                HttpContext.Session.SetString("Username", newUser.Username);
                HttpContext.Session.SetString("Role", newUser.Role);
                HttpContext.Session.SetInt32("PatientId", newUser.PatientId.Value); // newUser.PatientId is nullable, so use .Value
                HttpContext.Session.Remove("DoctorId"); // Ensure DoctorId is not set for a patient

                TempData["SuccessMessage"] = $"Registration successful! Your Username is: {newUser.Username} and Password is: {generatedPassword}. Please remember these for future logins.";
                Debug.WriteLine($"Patient '{newUser.Username}' successfully registered and logged in.");

                return RedirectToAction("Dashboard", "Patient");
            }

            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["LogoutMessage"] = "You have been successfully logged out.";
            Debug.WriteLine("AccountController.Logout: Session cleared, redirecting to Login.");
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["ErrorMessage"] = "You do not have permission to view this page.";
            ViewData["Title"] = "Access Denied";
            Debug.WriteLine("AccountController.AccessDenied: Access denied page shown.");
            return View();
        }
    }
}
