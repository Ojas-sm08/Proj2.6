using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.EntityFrameworkCore; // Required for ToListAsync()
using HospitalManagementSystem.Data;
using System.Diagnostics; // Added for Debug.WriteLine

namespace HospitalManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly HospitalDbContext _context;

        public AccountController(HospitalDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Clear any existing session data on accessing the login page
            HttpContext.Session.Clear();

            ViewData["Title"] = "User Login";
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery (CSRF)
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Please enter both username and password.";
                return View();
            }

            // Find the user by username (case-insensitive comparison)
            var user = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid username. Please try again.";
                return View();
            }

            // IMPORTANT SECURITY WARNING: In a real application, you must hash passwords
            // and compare the hash, not plain text passwords.
            // This is for demonstration purposes only.
            if (user.PasswordHash != password)
            {
                TempData["ErrorMessage"] = "Invalid password. Please enter a valid password.";
                return View();
            }

            // Set session variables upon successful login
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            TempData["SuccessMessage"] = $"Welcome, {user.Username}!";

            // Set specific IDs in session based on role
            if (user.Role == "Patient")
            {
                // Assuming patient's username is their name (e.g., "patient1" -> Patient name "patient1")
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Name.ToLower() == user.Username.ToLower());
                if (patient != null)
                {
                    HttpContext.Session.SetInt32("PatientId", patient.PatientId); // Store patient's specific ID
                }
                else
                {
                    // If a patient user exists but no matching patient profile, set PatientId to 0
                    HttpContext.Session.SetString("PatientId", "0");
                    TempData["ErrorMessage"] += " Could not find matching patient profile for this user.";
                    Debug.WriteLine($"Login (Patient Role): User '{user.Username}' logged in, but no matching Patient profile found for name '{user.Username}'.");
                }
                // For patient role, DoctorId is not applicable in session, set to "0"
                HttpContext.Session.SetString("DoctorId", "0");
            }
            else if (user.Role == "Doctor")
            {
                // REFINED LOGIC: Standardize username and doctor name for robust comparison
                // Example: username "dr.johndoe" -> "johndoe"
                string normalizedUsername = user.Username.Replace("dr.", "").Replace(".", "").ToLower();

                Debug.WriteLine($"Login (Doctor Role): User '{user.Username}' logged in. Normalized username for doctor lookup: '{normalizedUsername}'.");

                // Find doctor where their name, when normalized (e.g., "John Doe" -> "johndoe"), matches the normalized username
                var doctor = await _context.Doctors
                                           .FirstOrDefaultAsync(d => d.Name.Replace(" ", "").ToLower() == normalizedUsername);

                if (doctor != null)
                {
                    HttpContext.Session.SetInt32("DoctorId", doctor.Id); // Store doctor's specific ID using Doctor.Id
                    Debug.WriteLine($"Login (Doctor Role): Found matching doctor '{doctor.Name}' with ID: {doctor.Id}. DoctorId set in session.");
                }
                else
                {
                    // If a doctor user exists but no matching doctor profile, set DoctorId to 0
                    HttpContext.Session.SetString("DoctorId", "0");
                    TempData["ErrorMessage"] += " Doctor ID not found in session."; // This is the message you saw
                    Debug.WriteLine($"Login (Doctor Role): User '{user.Username}' logged in, but NO matching Doctor profile found for normalized name '{normalizedUsername}'.");
                    Debug.WriteLine("Please ensure Doctor.Name matches the username after removing 'dr.' and spaces, and converting to lowercase.");
                }
                // For doctor role, PatientId is not applicable in session, set to "0"
                HttpContext.Session.SetString("PatientId", "0");
            }
            else // For Admin or any other role
            {
                // For Admin or other roles, set both PatientId and DoctorId to "0"
                HttpContext.Session.SetString("PatientId", "0");
                HttpContext.Session.SetString("DoctorId", "0");
            }

            // Redirect based on user role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin"); // Assuming you have an AdminController
            }
            else if (user.Role == "Patient")
            {
                return RedirectToAction("Dashboard", "Patient");
            }
            else if (user.Role == "Doctor")
            {
                // Redirect to the Doctor's Dashboard
                // IMPORTANT: If DoctorId was not found, this will still try to redirect to Dashboard,
                // and the Dashboard action will then re-redirect to Login with the error.
                return RedirectToAction("Dashboard", "Doctor");
            }
            else
            {
                // Fallback for unhandled roles
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Account/Logout
        [HttpPost] // This is crucial for matching the form's method
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clears all session data
            TempData["LogoutMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Login", "Account"); // Redirect to login page after logout
        }

        // GET: /Account/AccessDenied (Optional: For displaying access denied messages)
        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["ErrorMessage"] = "You do not have permission to view this page."; // Use TempData for consistency
            ViewData["Title"] = "Access Denied";
            return View();
        }
    }
}
