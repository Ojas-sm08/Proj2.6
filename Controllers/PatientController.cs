using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using System.Collections.Generic;
// using Rotativa.AspNetCore; // Removed Rotativa using directive

// Added QuestPDF using directives
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing; // Added this using directive
using System.IO; // Required for MemoryStream


namespace HospitalManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly HospitalDbContext _context;

        public PatientController(HospitalDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsPatient() => HttpContext.Session.GetString("Role") == "Patient";


        // GET: /Patient/Manage (Your primary patient listing/management page)
        public async Task<IActionResult> Manage(string searchString, string gender)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            // Only Admins can access this management page.
            if (!IsAdmin())
            {
                return RedirectToAction("Dashboard", "Patient"); // Redirect non-admins
            }

            IQueryable<Patient> patients = _context.Patients;

            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p => p.Name.Contains(searchString) || p.ContactNumber.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(gender) && gender != "All")
            {
                patients = patients.Where(p => p.Gender == gender);
            }

            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentGender = gender;

            ViewBag.Genders = new List<string> { "All", "Male", "Female", "Other" };

            return View(await patients.ToListAsync());
        }

        // GET: /Patient/Notifications (New Action)
        public IActionResult Notifications()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Patient Notifications";
            return View(); // Renders Views/Patient/Notifications.cshtml
        }

        // GET: /Patient/ProfilePreview/{id?} (Modified Action to accept ID)
        public async Task<IActionResult> ProfilePreview(int? id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            string? role = HttpContext.Session.GetString("Role");
            Patient? patient = null;

            if (role == "Patient")
            {
                if (int.TryParse(HttpContext.Session.GetString("PatientId"), out int loggedInPatientId))
                {
                    patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == loggedInPatientId);
                }
            }
            else if (role == "Admin")
            {
                if (id.HasValue)
                {
                    patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id.Value);
                }
                else
                {
                    ViewBag.Message = "As an Admin, please select a patient from the 'Manage & Search Patients' list to view their profile.";
                }
            }

            ViewData["Title"] = "Patient Profile Preview";
            return View(patient);
        }

        // MODIFIED ACTION: GET: Patient/DownloadProfilePdf/{id} - Now generates a real PDF using QuestPDF
        [HttpGet]
        public async Task<IActionResult> DownloadProfilePdf(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            string? role = HttpContext.Session.GetString("Role");
            bool authorized = false;

            if (role == "Admin")
            {
                authorized = true;
            }
            else if (role == "Patient")
            {
                if (int.TryParse(HttpContext.Session.GetString("PatientId"), out int loggedInPatientId))
                {
                    if (loggedInPatientId == id)
                    {
                        authorized = true;
                    }
                }
            }

            if (!authorized)
            {
                TempData["ErrorMessage"] = "You are not authorized to download this profile.";
                return RedirectToAction("Login", "Account");
            }

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
            {
                return NotFound();
            }

            // --- REAL PDF GENERATION USING QUESTPDF ---
            // Set the QuestPDF license type. This is mandatory for commercial use but good practice for community.
            QuestPDF.Settings.License = LicenseType.Community;
            // Register a font for broader compatibility if needed (e.g., Arial).
            // FontManager.RegisterSystemFonts(); // Uncomment this line if you need system fonts
            // For simple cases, default fonts are often sufficient.

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Patient Profile")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2)
                        .AlignLeft();

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item().Text(text =>
                            {
                                text.Span("Generated On: ").SemiBold();
                                text.Span(DateTime.Now.ToShortDateString());
                            });

                            // CORRECTED: Moved PaddingVertical outside the Height and Background definition.
                            // This ensures the padding applies around the 1-unit high line.
                            column.Item().PaddingVertical(5).Height(1).Background(Colors.Grey.Lighten1);

                            column.Item().Text("Personal Information").FontSize(16).SemiBold().FontColor(Colors.Grey.Darken2);
                            column.Item().PaddingLeft(10).Column(col =>
                            {
                                col.Spacing(5);
                                col.Item().Text(text => { text.Span("Name: ").SemiBold(); text.Span(patient.Name); });
                                col.Item().Text(text => { text.Span("Patient ID: ").SemiBold(); text.Span(patient.PatientId.ToString()); });
                                col.Item().Text(text => { text.Span("Date of Birth: ").SemiBold(); text.Span(patient.DateOfBirth.ToShortDateString()); });
                                col.Item().Text(text => { text.Span("Gender: ").SemiBold(); text.Span(patient.Gender); });
                            });

                            // CORRECTED: Moved PaddingVertical outside the Height and Background definition.
                            column.Item().PaddingVertical(5).Height(1).Background(Colors.Grey.Lighten1);

                            column.Item().Text("Contact Information").FontSize(16).SemiBold().FontColor(Colors.Grey.Darken2);
                            column.Item().PaddingLeft(10).Column(col =>
                            {
                                col.Spacing(5);
                                col.Item().Text(text => { text.Span("Contact Number: ").SemiBold(); text.Span(patient.ContactNumber); });
                                col.Item().Text(text => { text.Span("Address: ").SemiBold(); text.Span(patient.Address); });
                            });

                            // CORRECTED: Moved PaddingVertical outside the Height and Background definition.
                            column.Item().PaddingVertical(5).Height(1).Background(Colors.Grey.Lighten1);

                            column.Item().Text("Medical History").FontSize(16).SemiBold().FontColor(Colors.Grey.Darken2);
                            column.Item().PaddingLeft(10).Text(text => text.Span(patient.MedicalHistory ?? "N/A"));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            // Using static placeholder for page numbers as discussed
                            x.Span("Page X of Y").FontSize(10);
                        });
                });
            });

            // Generate the PDF into a MemoryStream
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0; // Reset stream position to the beginning

            // Return the PDF file
            return File(stream.ToArray(), "application/pdf", $"PatientProfile_{patient.Name.Replace(" ", "_")}.pdf");
        }


        // Example of a Patient Dashboard (for a logged-in patient)
        public IActionResult Dashboard()
        {
            if (!IsLoggedIn() || !IsPatient())
            {
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Patient Dashboard";
            return View(); // Renders Views/Patient/Dashboard.cshtml
        }

        // Example of a Patient's Appointments list
        public async Task<IActionResult> Appointments()
        {
            if (!IsLoggedIn() || !IsPatient())
            {
                return RedirectToAction("Login", "Account");
            }

            var patientIdString = HttpContext.Session.GetString("PatientId");
            if (!int.TryParse(patientIdString, out int loggedInPatientId))
            {
                TempData["ErrorMessage"] = "Could not retrieve patient ID from session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var appointments = await _context.Appointments
                                     .Include(a => a.Doctor) // Include Doctor details
                                     .Where(a => a.PatientId == loggedInPatientId)
                                     .OrderBy(a => a.Date)
                                     .ThenBy(a => a.Time)
                                     .ToListAsync();

            ViewData["Title"] = "My Appointments";
            return View("Appointments", appointments); // Renders Views/Patient/Appointments.cshtml
        }

        // GET for creating a new patient:
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn() || !IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View();
        }

        // POST for creating a new patient:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,DateOfBirth,Gender,ContactNumber,Address,MedicalHistory")] Patient patient)
        {
            if (!IsLoggedIn() || !IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Patient added successfully!";
                return RedirectToAction(nameof(Manage));
            }
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View(patient);
        }

        // GET: Patient/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var patient = await _context.Patients
                                        .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // GET: Patient/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin()) return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View(patient);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,Name,DateOfBirth,Gender,ContactNumber,Address,MedicalHistory")] Patient patient)
        {
            if (!IsLoggedIn() || !IsAdmin()) return RedirectToAction("Login", "Account");

            if (id != patient.PatientId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Patient updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Manage));
            }
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View(patient);
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }

        // GET: Patient/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin()) return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var patient = await _context.Patients.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin()) return RedirectToAction("Login", "Account");

            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Patient deleted successfully!";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}
