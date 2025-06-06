using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // For session access

namespace HospitalManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        // GET: /Admin/Index (or any other default admin action)
        public IActionResult Index()
        {
            // Basic check to ensure only Admins can access this dashboard
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                // If not Admin, redirect to login
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Admin Dashboard"; // Set title for the view
            return View(); // This will render Views/Admin/Index.cshtml
        }

        // You can add more admin-specific actions here later (e.g., UserManagement, Reports)
    }
}
