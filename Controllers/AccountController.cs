using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.EntityFrameworkCore; // Required for FirstOrDefaultAsync, ToListAsync etc.
using HospitalManagementSystem.Data; // Required for HospitalDbContext
// Removed: using System.Diagnostics; // Debugging statements removed

namespace HospitalManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly HospitalDbContext _context; // Use the injected DbContext

        public AccountController(HospitalDbContext context)
        {
            _context = context; // Initialize DbContext
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Clear any existing session data on accessing the login page to ensure a fresh state
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

            var user = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View();
            }

            // IMPORTANT SECURITY WARNING: In a real application, you MUST hash passwords
            // and compare the hash, not plain text passwords.
            // This implementation is for demonstration purposes only.
            if (user.PasswordHash != password) // Comparing plain text password (INSECURE for production)
            {
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View();
            }

            // Set session variables upon successful login
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);


            // Set specific IDs in session based on role
            if (user.Role == "Patient")
            {
                string normalizedPatientUsername = user.Username.ToLower();
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Name.ToLower() == normalizedPatientUsername);
                if (patient != null)
                {
                    HttpContext.Session.SetInt32("PatientId", patient.PatientId); // Using SetInt32
                }
                else
                {
                    HttpContext.Session.Clear();
                    TempData["ErrorMessage"] = "Patient profile not found for this user. Please contact support.";
                    return View();
                }
            }
            else if (user.Role == "Doctor")
            {
                string normalizedUserIdentifier = user.Username.Replace("dr.", "").Replace(".", "").ToLower();

                var doctor = await _context.Doctors
                                           .FirstOrDefaultAsync(d => d.Name.Replace(" ", "").ToLower() == normalizedUserIdentifier);

                if (doctor != null)
                {
                    // Storing DoctorId as a string in session to avoid byte corruption issues
                    HttpContext.Session.SetString("DoctorId", doctor.Id.ToString());
                }
                else
                {
                    HttpContext.Session.Clear();
                    TempData["ErrorMessage"] = "Doctor profile not found for this user. Please contact support. Ensure doctor's name matches user's name in the database.";
                    return View();
                }
            }

            TempData["SuccessMessage"] = $"Welcome, {user.Username}!";

            // Redirect based on user role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.Role == "Patient")
            {
                return RedirectToAction("Dashboard", "Patient");
            }
            else if (user.Role == "Doctor")
            {
                // Final redirect target for Doctor role
                return RedirectToAction("Dashboard", "Doctor");
            }
            else
            {
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
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            ViewBag.ErrorMessage = "You do not have permission to view this page.";
            ViewData["Title"] = "Access Denied";
            return View();
        }
    }
}
