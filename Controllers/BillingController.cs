using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitalManagementSystem.Controllers
{
    public class BillingController : Controller
    {
        private static List<Billing> bills = new List<Billing>
        {
            new Billing { Id = 1, PatientName = "John Doe", Treatment = "X-ray", Amount = 1200, BillingDate = DateTime.Today },
            new Billing { Id = 2, PatientName = "Jane Smith", Treatment = "Consultation", Amount = 800, BillingDate = DateTime.Today.AddDays(-1) }
        };

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            return View(bills);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Billing bill)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                bill.Id = bills.Count > 0 ? bills.Max(b => b.Id) + 1 : 1;
                bills.Add(bill);
                TempData["Message"] = "Bill added successfully!";
                return RedirectToAction("Index");
            }

            return View(bill);
        }
    }
}
