using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.EntityFrameworkCore; // Required for ToListAsync(), FirstOrDefaultAsync()
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
            // If already logged in, redirect to appropriate dashboard immediately
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                var role = HttpContext.Session.GetString("Role");
                if (role == "Admin")
                {
                    Debug.WriteLine("AccountController.Login (GET): User is Admin, redirecting to Admin/Index.");
                    return RedirectToAction("Index", "Admin");
                }
                else if (role == "Doctor")
                {
                    Debug.WriteLine("AccountController.Login (GET): User is Doctor, redirecting to Doctor/Dashboard.");
                    return RedirectToAction("Dashboard", "Doctor"); // Redirect doctors to their dashboard
                }
                else if (role == "Patient")
                {
                    Debug.WriteLine("AccountController.Login (GET): User is Patient, redirecting to Patient/Dashboard.");
                    return RedirectToAction("Dashboard", "Patient");
                }
            }
            // Clear any existing session data if not logged in (to ensure a fresh login)
            HttpContext.Session.Clear(); // Only clear if not already logged in
            ViewData["Title"] = "User Login";
            Debug.WriteLine("AccountController.Login (GET): User not logged in, showing login view.");
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
                Debug.WriteLine("AccountController.Login (POST): Missing username or password.");
                return View();
            }

            var user = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid username. Please try again.";
                Debug.WriteLine($"AccountController.Login (POST): Invalid username '{username}'.");
                return View();
            }

            if (user.PasswordHash != password)
            {
                TempData["ErrorMessage"] = "Invalid password. Please enter a valid password.";
                Debug.WriteLine($"AccountController.Login (POST): Invalid password for user '{username}'.");
                return View();
            }

            // Set session variables upon successful login
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            // Set specific IDs in session based on role, using SetInt32
            if (user.Role == "Patient")
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Name.ToLower() == user.Username.ToLower());
                if (patient != null)
                {
                    HttpContext.Session.SetInt32("PatientId", patient.PatientId);
                    Debug.WriteLine($"AccountController.Login (POST): Patient '{user.Username}' logged in. PatientId: {patient.PatientId} set in session.");
                }
                else
                {
                    HttpContext.Session.SetInt32("PatientId", 0);
                    TempData["ErrorMessage"] += " Could not find matching patient profile for this user.";
                    Debug.WriteLine($"AccountController.Login (POST): User '{user.Username}' logged in, but no matching Patient profile found. PatientId set to 0.");
                }
                HttpContext.Session.SetInt32("DoctorId", 0);
            }
            else if (user.Role == "Doctor")
            {
                string normalizedUsername = user.Username.Replace("dr.", "").Replace(".", "").ToLower();
                Debug.WriteLine($"AccountController.Login (POST): Doctor '{user.Username}' logging in. Normalized username for doctor lookup: '{normalizedUsername}'.");

                var doctor = await _context.Doctors
                                           .FirstOrDefaultAsync(d => d.Name.Replace(" ", "").ToLower() == normalizedUsername);

                if (doctor != null)
                {
                    HttpContext.Session.SetInt32("DoctorId", doctor.Id);
                    Debug.WriteLine($"AccountController.Login (POST): Found matching doctor '{doctor.Name}' with ID: {doctor.Id}. DoctorId set in session.");
                }
                else
                {
                    HttpContext.Session.SetInt32("DoctorId", 0);
                    TempData["ErrorMessage"] += " Could not find matching doctor profile for this user.";
                    Debug.WriteLine($"AccountController.Login (POST): User '{user.Username}' logged in, but NO matching Doctor profile found. DoctorId set to 0.");
                }
                HttpContext.Session.SetInt32("PatientId", 0);
            }
            else // For Admin or any other role
            {
                HttpContext.Session.SetInt32("PatientId", 0);
                HttpContext.Session.SetInt32("DoctorId", 0);
                Debug.WriteLine($"AccountController.Login (POST): Admin/Other role '{user.Username}' logged in. PatientId/DoctorId set to 0.");
            }

            TempData["SuccessMessage"] = $"Welcome, {user.Username}!";
            Debug.WriteLine($"AccountController.Login (POST): Successful login for '{user.Username}' with role '{user.Role}'.");

            // Redirect based on user role
            if (user.Role == "Admin")
            {
                Debug.WriteLine("AccountController.Login (POST): Redirecting to Admin/Index.");
                return RedirectToAction("Index", "Admin");
            }
            else if (user.Role == "Patient")
            {
                Debug.WriteLine("AccountController.Login (POST): Redirecting to Patient/Dashboard.");
                return RedirectToAction("Dashboard", "Patient");
            }
            else if (user.Role == "Doctor")
            {
                Debug.WriteLine("AccountController.Login (POST): Redirecting to Doctor/Dashboard.");
                return RedirectToAction("Dashboard", "Doctor"); // THIS SHOULD BE THE LANDING PAGE FOR DOCTORS
            }
            else
            {
                Debug.WriteLine("AccountController.Login (POST): Unhandled role, redirecting to Home/Index.");
                return RedirectToAction("Index", "Home");
            }
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
