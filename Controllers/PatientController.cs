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
using System.IO; // For MemoryStream
using System.Globalization; // For CultureInfo and DateTimeStyles (if still needed for appointments)

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
        // Allows Admin to edit any patient, and Patient to edit their own profile.
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to edit profiles.";
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = HttpContext.Session.GetInt32("PatientId");
            var currentUserRole = HttpContext.Session.GetString("Role");

            // Authorization logic:
            // 1. Admin can edit any patient.
            // 2. Patient can ONLY edit their own profile (id must match PatientId in session).
            if (currentUserRole == "Admin")
            {
                // Admin is authorized, proceed.
            }
            else if (currentUserRole == "Patient")
            {
                if (id == null || id == 0 || id != currentUserId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this patient's profile.";
                    return RedirectToAction("ProfilePreview", "Patient"); // Redirect patient to their profile preview
                }
            }
            else // Doctor or other unauthorized roles
            {
                TempData["ErrorMessage"] = "You are not authorized to edit patient profiles.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            ViewData["Title"] = $"Edit Patient: {patient.Name}";
            return View(patient);
        }

        // POST: Patient/Edit/5
        // Handles submission of patient profile edits.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int PatientId, [Bind("PatientId,Name,DateOfBirth,Gender,ContactNumber,Email,Address,MedicalHistory")] Patient patient)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to edit profiles.";
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = HttpContext.Session.GetInt32("PatientId");
            var currentUserRole = HttpContext.Session.GetString("Role");

            // Authorization logic:
            // 1. Admin can edit any patient.
            // 2. Patient can ONLY edit their own profile (PatientId must match PatientId in session).
            if (currentUserRole == "Admin")
            {
                // Admin is authorized, proceed.
            }
            else if (currentUserRole == "Patient")
            {
                if (PatientId != currentUserId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this patient's profile.";
                    return RedirectToAction("ProfilePreview", "Patient"); // Redirect patient to their profile preview
                }
            }
            else // Doctor or other unauthorized roles
            {
                TempData["ErrorMessage"] = "You are not authorized to edit patient profiles.";
                return RedirectToAction("Login", "Account");
            }

            if (PatientId != patient.PatientId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Profile for {patient.Name} updated successfully!"; // More user-friendly message
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

                if (currentUserRole == "Patient")
                {
                    Debug.WriteLine($"PatientController.Edit (POST): Patient '{patient.Name}' edited their profile, redirecting to ProfilePreview.");
                    return RedirectToAction("ProfilePreview", "Patient");
                }
                else // Implies Admin based on earlier authorization
                {
                    Debug.WriteLine($"PatientController.Edit (POST): Admin edited patient '{patient.Name}', redirecting to Manage.");
                    return RedirectToAction(nameof(Manage));
                }
            }
            ViewData["Title"] = $"Edit Patient: {patient.Name}";
            return View(patient); // If ModelState is not valid, redisplay the form with errors
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
                    return RedirectToAction("ProfilePreview", "Patient"); // Redirect patient to their own profile if trying to view others
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

        // GET: Patient/Dashboard
        public IActionResult Dashboard()
        {
            // Ensure the user is logged in as a patient to access this dashboard
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Patient")
            {
                TempData["ErrorMessage"] = "You must be logged in as a patient to access the dashboard.";
                return RedirectToAction("Login", "Account", new { role = "Patient" }); // Redirect to patient login
            }

            ViewData["Title"] = "Patient Dashboard";
            ViewBag.Username = username?.Replace("pat.", ""); // Pass the cleaned username to the view
            Debug.WriteLine($"PatientController.Dashboard: User '{username}' ({role}) accessed dashboard.");
            return View();
        }

        // NEW ACTION: Patient/ProfilePreview for logged-in patient's own info
        [HttpGet]
        public async Task<IActionResult> ProfilePreview()
        {
            // 1. Check if logged in and if role is Patient
            if (!IsLoggedIn() || !IsPatient())
            {
                TempData["ErrorMessage"] = "You must be logged in as a patient to view your profile.";
                return RedirectToAction("Login", "Account");
            }

            // 2. Get the PatientId from session
            var patientIdInSession = HttpContext.Session.GetInt32("PatientId");
            if (patientIdInSession == null || patientIdInSession == 0)
            {
                TempData["ErrorMessage"] = "Patient ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // 3. Fetch the patient details
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patientIdInSession);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Your patient profile could not be found.";
                // Optionally, log this as an unusual event if a logged-in patient's ID is missing
                return RedirectToAction("Dashboard", "Patient"); // Redirect to dashboard or login
            }

            // 4. Fetch associated patient files (if any, for future use/display)
            var patientFiles = await _context.PatientFiles
                                             .Where(pf => pf.PatientId == patient.PatientId)
                                             .OrderByDescending(pf => pf.UploadDate)
                                             .ToListAsync();

            // 5. Create a ViewModel to pass to the view
            var viewModel = new PatientDetailsViewModel
            {
                Patient = patient,
                PatientFiles = patientFiles
            };

            ViewData["Title"] = "My Info"; // Set the title for the view
            return View(viewModel); // Pass the ViewModel to the ProfilePreview.cshtml view
        }

        // MODIFIED ACTION: Get all doctors for Patient to view (and Admin to manage)
        [HttpGet]
        public async Task<IActionResult> DoctorList(string searchString, string specialization)
        {
            // Authorization: Allow all logged-in users to view, but only Admin to manage.
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view doctors.";
                return RedirectToAction("Login", "Account");
            }

            // Set dynamic title based on role for clarity
            ViewData["Title"] = IsAdmin() ? "Manage Doctors" : "Our Doctors";

            IQueryable<Doctor> doctors = _context.Doctors;

            // Apply search filter if present
            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString) || d.Contact.Contains(searchString));
            }

            // Apply specialization filter if present and not "All Specializations"
            if (!string.IsNullOrEmpty(specialization))
            {
                doctors = doctors.Where(d => d.Specialization == specialization);
            }

            // Populate ViewBag.Specializations for the dropdown filter
            ViewBag.Specializations = await _context.Doctors
                                                    .Select(d => d.Specialization)
                                                    .Where(s => !string.IsNullOrEmpty(s)) // Exclude null/empty specializations
                                                    .Distinct() // Get only unique specializations
                                                    .OrderBy(s => s) // Sort alphabetically
                                                    .ToListAsync();

            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentSpecialization = specialization; // Pass the selected specialization back to the view

            return View(await doctors.ToListAsync());
        }


        // NEW ACTION: DownloadProfilePdf
        [HttpGet]
        public async Task<IActionResult> DownloadProfilePdf(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to download patient profiles.";
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = HttpContext.Session.GetInt32("PatientId");
            var currentUserRole = HttpContext.Session.GetString("Role");

            // Authorization logic for PDF download:
            // Admin can download any patient's PDF.
            // Patient can ONLY download their own profile PDF (id must match PatientId in session).
            // Doctor can download PDF for patients they have appointments with (this logic is in Details, you might want to adapt it).
            if (currentUserRole == "Admin")
            {
                // Admin is authorized, proceed.
            }
            else if (currentUserRole == "Patient")
            {
                if (id == null || id == 0 || id != currentUserId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to download this patient's profile.";
                    return RedirectToAction("ProfilePreview", "Patient");
                }
            }
            else // Doctor or other unauthorized roles
            {
                // For Doctors, you would need to implement similar logic as in the Details action
                // to check if they have an appointment with this patient.
                // For now, we'll restrict it if not Admin or the patient themselves for simplicity.
                TempData["ErrorMessage"] = "You are not authorized to download patient profiles.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
            if (patient == null) return NotFound();

            // --- PDF Generation Logic (Placeholder) ---
            // In a real application, you would use a PDF generation library here (e.g., QuestPDF, iTextSharp, IronPDF).
            // For demonstration, we'll create a simple text file and return it as a PDF.

            string content = $"Patient Profile\n\n" +
                             $"Name: {patient.Name}\n" +
                             $"Patient ID: {patient.PatientId}\n" +
                             $"Date of Birth: {patient.DateOfBirth.ToShortDateString()}\n" +
                             $"Gender: {patient.Gender}\n" +
                             $"Contact: {patient.ContactNumber}\n" +
                             $"Email: {patient.Email}\n" +
                             $"Address: {patient.Address}\n" +
                             $"Medical History: {patient.MedicalHistory}\n\n" +
                             $"Generated on: {DateTime.Now}";

            var fileName = $"Patient_Profile_{patient.Name.Replace(" ", "_")}_{patient.PatientId}.pdf";
            var contentType = "application/pdf"; // Set the correct content type for PDF

            // Create a MemoryStream to hold the PDF data
            using (var memoryStream = new MemoryStream())
            {
                // In a real scenario, PDF library would write to this stream.
                // For demo, write content as bytes.
                var writer = new StreamWriter(memoryStream);
                writer.Write(content);
                writer.Flush(); // Ensure all buffered text is written to the stream
                memoryStream.Position = 0; // Reset stream position to the beginning

                return File(memoryStream.ToArray(), contentType, fileName);
            }
        }
        // END NEW ACTION

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}
