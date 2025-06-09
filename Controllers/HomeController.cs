using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HospitalManagementSystem.Models;
using Hospital_Management_System.Models; // Ensure this is present if you use any models here

namespace HospitalManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // The Index action for your main landing page.
        // By default, this action does NOT require authentication, making it suitable as a landing page.
        // You generally don't need [AllowAnonymous] here unless you've set a global [Authorize] filter.
        public IActionResult Index()
        {
            ViewData["Title"] = "Welcome to Hospital Management System"; // Customize your page title
            return View();
        }

        // Example Privacy page (often requires authentication for sensitive info, but typically public)
        public IActionResult Privacy()
        {
            ViewData["Title"] = "Privacy Policy";
            return View();
        }

        // Error handling page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
