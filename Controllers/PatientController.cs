using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For HttpContext.Session
using System.Collections.Generic; // For List
using System.Diagnostics; // For Debug.WriteLine
using System; // For Exception

namespace HospitalManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly HospitalDbContext _context;

        public PatientController(HospitalDbContext context)
        {
            _context = context;
        }

        // Helper method for session/role checks
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";
        private bool IsPatient() => HttpContext.Session.GetString("Role") == "Patient";


        // GET: Patient/Manage (Admin View to Manage Patients)
        [HttpGet]
        public async Task<IActionResult> Manage(string searchString, string genderFilter)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to manage patients.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Manage Patients";
            IQueryable<Patient> patients = _context.Patients;

            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p => p.Name.Contains(searchString) ||
                                                 p.ContactNumber.Contains(searchString) ||
                                                 (p.Email != null && p.Email.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(genderFilter) && genderFilter != "All")
            {
                patients = patients.Where(p => p.Gender == genderFilter);
            }

            ViewBag.Genders = await _context.Patients
                                            .Select(p => p.Gender)
                                            .Distinct()
                                            .OrderBy(g => g)
                                            .ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentGenderFilter"] = genderFilter;

            return View(await patients.ToListAsync());
        }

        // GET: Patient/Create (Admin creates new patient)
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to add new patients.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Add New Patient";
            return View();
        }

        // POST: Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,DateOfBirth,Gender,ContactNumber,Email,Address,MedicalHistory")] Patient patient)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to add new patients.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Patient {patient.Name} added successfully!";
                return RedirectToAction(nameof(Manage));
            }
            ViewData["Title"] = "Add New Patient";
            return View(patient);
        }

        // GET: Patient/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit patients.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            ViewData["Title"] = $"Edit Patient: {patient.Name}";
            return View(patient);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int PatientId, [Bind("PatientId,Name,DateOfBirth,Gender,ContactNumber,Email,Address,MedicalHistory")] Patient patient)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit patients.";
                return RedirectToAction("Login", "Account");
            }

            if (PatientId != patient.PatientId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Patient {patient.Name} updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Manage));
            }
            ViewData["Title"] = $"Edit Patient: {patient.Name}";
            return View(patient);
        }

        // GET: Patient/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to delete patients.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var patient = await _context.Patients
                                        .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            ViewData["Title"] = $"Delete Patient: {patient.Name}";
            return View(patient);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized to delete patients." });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var patient = await _context.Patients.FindAsync(id);
                    if (patient == null)
                    {
                        return Json(new { success = false, message = "Patient not found." });
                    }

                    // Check for associated appointments
                    var appointments = await _context.Appointments
                                                     .Where(a => a.PatientId == patient.PatientId)
                                                     .ToListAsync();
                    if (appointments.Any())
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"Cannot delete patient {patient.Name}. There are {appointments.Count} existing appointments linked to this patient. Please delete or reassign appointments first." });
                    }

                    // Check for associated patient files
                    var patientFiles = await _context.PatientFiles
                                                     .Where(pf => pf.PatientId == patient.PatientId)
                                                     .ToListAsync();
                    if (patientFiles.Any())
                    {
                        _context.PatientFiles.RemoveRange(patientFiles); // Remove all associated files
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted {patientFiles.Count} files for patient ID: {id}");
                    }

                    // Check for associated user account (if patient has a user login)
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.PatientId == patient.PatientId);
                    if (user != null)
                    {
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted associated user for patient ID: {id}, Username: {user.Username}");
                    }
                    else
                    {
                        Debug.WriteLine($"No associated user found for patient ID: {id}");
                    }


                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    return Json(new { success = true, message = $"Patient {patient.Name} and associated data deleted successfully!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"Error deleting patient: {ex.Message}");
                    return Json(new { success = false, message = $"Error deleting patient: {ex.Message}" });
                }
            }
        }


        // GET: Patient/Details/5 (Allows Doctor/Admin/Patient to view a patient's details)
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor() && !IsPatient()))
            {
                TempData["ErrorMessage"] = "You must be logged in as an authorized user to view patient details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                                        .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null)
            {
                return NotFound();
            }

            // Specific authorization based on role:
            if (IsDoctor())
            {
                var doctorId = HttpContext.Session.GetInt32("DoctorId");
                Debug.WriteLine($"PatientController.Details: DoctorId from session for current doctor: {doctorId}");

                if (doctorId == null || doctorId == 0) // Check for 0 as well, indicating no actual DoctorId
                {
                    TempData["ErrorMessage"] = "Doctor ID not found in session or invalid. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                var hasAppointmentWithThisDoctor = await _context.Appointments
                                                                 .AnyAsync(a => a.DoctorId == doctorId && a.PatientId == patient.PatientId);

                if (!hasAppointmentWithThisDoctor)
                {
                    TempData["ErrorMessage"] = "You are not authorized to view this patient's details as they are not listed as your patient.";
                    return RedirectToAction("Dashboard", "Doctor");
                }
            }
            else if (IsPatient())
            {
                var patientIdInSession = HttpContext.Session.GetInt32("PatientId");
                if (id != patientIdInSession)
                {
                    TempData["ErrorMessage"] = "You are not authorized to view other patient's details.";
                    return RedirectToAction("Dashboard", "Patient");
                }
            }

            var patientFiles = await _context.PatientFiles
                                             .Where(pf => pf.PatientId == patient.PatientId)
                                             .OrderByDescending(pf => pf.UploadDate)
                                             .ToListAsync();

            var viewModel = new PatientDetailsViewModel
            {
                Patient = patient,
                PatientFiles = patientFiles
            };

            ViewData["Title"] = $"Patient Details: {patient.Name}";
            return View(viewModel);
        }

        // GET: Patient/Notifications (Admin view for patient-related notifications)
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to view patient notifications.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Patient Notifications";

            var notifications = await _context.Notifications
                                              .Include(n => n.Patient)
                                              .OrderByDescending(n => n.CreatedDate)
                                              .ToListAsync();

            return View(notifications);
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}
