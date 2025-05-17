using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HospitalManagementSystem.Models;
using System.Collections.Generic;
using System.Linq;

namespace HospitalManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        // Dummy user data for authentication (replace with DB later)
        private static List<User> users = new List<User>
        {
            new User { Id = 1, Username = "admin", Password = "admin123", Role = "Admin" },
            new User { Id = 2, Username = "doctor1", Password = "doc123", Role = "Doctor" },
            new User { Id = 3, Username = "patient1", Password = "pat123", Role = "Patient" }
        };

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = users.FirstOrDefault(u =>
                    u.Username == model.Username && u.Password == model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    // Redirect based on role
                    return user.Role switch
                    {
                        "Admin" => RedirectToAction("Index", "Admin"),
                        "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                        "Patient" => RedirectToAction("Dashboard", "Patient"),
                        _ => RedirectToAction("Login", "Account")
                    };
                }

                ViewBag.Error = "Invalid username or password.";
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["LogoutMessage"] = "You have successfully logged out!";
            return RedirectToAction("Login", "Account");
        }
    }
}
