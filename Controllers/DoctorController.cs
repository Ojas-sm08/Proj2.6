using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For HttpContext.Session
using Microsoft.EntityFrameworkCore; // For Entity Framework Core operations
using HospitalManagementSystem.Data; // Assuming your DbContext is here
using System.Diagnostics; // For Debug.WriteLine
using System; // For Exception
using System.Collections.Generic; // For List

namespace HospitalManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly HospitalDbContext _context;
        private readonly Random _random; // For generating random times

        public DoctorController(HospitalDbContext context)
        {
            _context = context;
            _random = new Random(); // Initialize Random
        }

        // Helper methods for session/role checks
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";

        // GET: Doctor/Index (Public Doctor Directory for Patients/Anyone)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Doctor Directory";
            var doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
            return View(doctors);
        }

        // GET: Doctor/Manage (Admin View to Manage Doctors)
        [HttpGet]
        public async Task<IActionResult> Manage(string searchString, string specialization)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to manage doctors.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Manage Doctors";
            IQueryable<Doctor> doctors = _context.Doctors;

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString) || d.Contact.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(specialization) && specialization != "All")
            {
                doctors = doctors.Where(d => d.Specialization == specialization);
            }

            ViewBag.Specializations = await _context.Doctors
                                                    .Select(d => d.Specialization)
                                                    .Distinct()
                                                    .OrderBy(s => s)
                                                    .ToListAsync();

            return View(await doctors.ToListAsync());
        }

        // GET: Doctor/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to add new doctors.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Add New Doctor";
            return View();
        }

        // POST: Doctor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor, string username, string password)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to add new doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("username", "A login account with this username already exists.");
                TempData["ErrorMessage"] = $"Login account '{username}' already exists. Please choose a different name for the doctor or delete the existing user.";
                ViewData["Title"] = "Add New Doctor";
                return View(doctor);
            }

            if (ModelState.IsValid)
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _context.Add(doctor);
                        await _context.SaveChangesAsync();

                        var user = new User
                        {
                            Username = username,
                            PasswordHash = password,
                            Role = "Doctor",
                            DoctorId = doctor.Id
                        };
                        _context.Add(user);
                        await _context.SaveChangesAsync();

                        transaction.Commit();
                        TempData["SuccessMessage"] = $"Dr. {doctor.Name} added successfully and login account '{username}' created.";
                        return RedirectToAction(nameof(Manage));
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = $"Error adding doctor and user: {ex.Message}";
                        Debug.WriteLine($"Error adding doctor and user: {ex.Message}");
                        ViewData["Title"] = "Add New Doctor";
                        return View(doctor);
                    }
                }
            }
            ViewData["Title"] = "Add New Doctor";
            return View(doctor);
        }

        // GET: Doctor/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == doctor.Id);
            ViewBag.Username = user?.Username;

            ViewData["Title"] = $"Edit Dr. {doctor.Name}";
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Specialization,Description,Contact,Location")] Doctor doctor, string username, string password)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id != doctor.Id) return NotFound();

            if (username != null)
            {
                var existingUserWithUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.DoctorId != doctor.Id);
                if (existingUserWithUsername != null)
                {
                    ModelState.AddModelError("username", "A login account with this username already exists for another user.");
                    TempData["ErrorMessage"] = $"Login account '{username}' already exists for another user. Changes rolled back.";
                    ViewBag.Username = username;
                    ViewData["Title"] = $"Edit Dr. {doctor.Name}";
                    return View(doctor);
                }
            }

            if (ModelState.IsValid)
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _context.Update(doctor);
                        await _context.SaveChangesAsync();

                        var user = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == doctor.Id);
                        if (user != null)
                        {
                            user.Username = username;
                            if (!string.IsNullOrEmpty(password))
                            {
                                user.PasswordHash = password;
                            }
                            _context.Update(user);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            var newUser = new User
                            {
                                Username = username,
                                PasswordHash = !string.IsNullOrEmpty(password) ? password : "DefaultSecurePassword",
                                Role = "Doctor",
                                DoctorId = doctor.Id
                            };
                            _context.Users.Add(newUser);
                            await _context.SaveChangesAsync();
                            Debug.WriteLine($"Created new user for doctor ID: {doctor.Id} during edit. Username: {username}");
                        }

                        transaction.Commit();
                        TempData["SuccessMessage"] = $"Dr. {doctor.Name} updated successfully!";
                        return RedirectToAction(nameof(Manage));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!DoctorExists(doctor.Id))
                        {
                            transaction.Rollback();
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = $"Error updating doctor: {ex.Message}";
                        Debug.WriteLine($"Error updating doctor and/or user: {ex.Message}");
                        ViewBag.Username = username;
                        ViewData["Title"] = $"Edit Dr. {doctor.Name}";
                        return View(doctor);
                    }
                }
            }
            ViewBag.Username = username;
            ViewData["Title"] = $"Edit Dr. {doctor.Name}";
            return View(doctor);
        }

        // GET: Doctor/Details/5 (Optional: displays doctor details)
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();
            ViewData["Title"] = $"Details of Dr. {doctor.Name}";
            return View(doctor);
        }

        // GET: Doctor/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                TempData["ErrorMessage"] = "You are not authorized to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            ViewData["Title"] = $"Delete Dr. {doctor.Name}";
            return View(doctor);
        }

        // POST: Doctor/Delete/5 (Handles both Doctor and associated User deletion)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || !IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized to delete doctors." });
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                    {
                        return Json(new { success = false, message = "Doctor not found." });
                    }

                    var appointments = await _context.Appointments
                                                     .Where(a => a.DoctorId == doctor.Id)
                                                     .ToListAsync();
                    if (appointments.Any())
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"Cannot delete Dr. {doctor.Name}. There are {appointments.Count} existing appointments linked to this doctor. Please delete or reassign appointments first." });
                    }

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == doctor.Id);
                    if (user != null)
                    {
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();
                        Debug.WriteLine($"Deleted associated user for doctor ID: {id}, Username: {user.Username}");
                    }
                    else
                    {
                        Debug.WriteLine($"No associated user found for doctor ID: {id}");
                    }

                    _context.Doctors.Remove(doctor);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    return Json(new { success = true, message = $"Dr. {doctor.Name} and associated user account deleted successfully!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"Error deleting doctor and/or user: {ex.Message}");
                    return Json(new { success = false, message = $"Error deleting doctor: {ex.Message}" });
                }
            }
        }

        // GET: Doctor/Dashboard
        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You are not authorized to view the doctor dashboard.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Doctor Dashboard";
            return View();
        }

        // GET: Doctor/Schedule
        [HttpGet]
        public async Task<IActionResult> Schedule(string selectedDate)
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You are not authorized to view the doctor schedule.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == HttpContext.Session.GetString("Username"));
            int doctorId = 0;

            if (currentUser != null && currentUser.DoctorId.HasValue)
            {
                doctorId = currentUser.DoctorId.Value;
            }
            else
            {
                TempData["ErrorMessage"] = "Your doctor profile could not be found or is incomplete. Please contact support or log in again.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Associated doctor profile not found in the Doctors table. Please contact support.";
                return RedirectToAction("Login", "Account");
            }

            DateTime dateToDisplay;
            if (!DateTime.TryParse(selectedDate, out dateToDisplay))
            {
                dateToDisplay = DateTime.Today;
            }
            ViewData["SelectedDate"] = dateToDisplay.ToString("yyyy-MM-dd");

            var doctorSchedule = await _context.DoctorSchedules
                                            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.Date.Date == dateToDisplay.Date);

            if (doctorSchedule == null)
            {
                var startHour = _random.Next(8, 11);
                var startTime = new TimeSpan(startHour, _random.Next(0, 60), 0);

                var minEndHour = Math.Max(startTime.Hours + 6, 16);
                var maxEndHour = 18;
                var endHour = _random.Next(minEndHour, maxEndHour + 1);
                var endTime = new TimeSpan(endHour, _random.Next(0, 60), 0);

                var lunchStartHour = _random.Next(12, 15);
                var lunchStartTime = new TimeSpan(lunchStartHour, _random.Next(0, 60), 0);
                var lunchEndTime = lunchStartTime.Add(new TimeSpan(1, 0, 0));

                string locationBase = doctor.Specialization?.Replace(" ", "") ?? "Clinic";
                string doctorInitials = "DR";

                if (!string.IsNullOrEmpty(doctor.Name))
                {
                    Debug.WriteLine($"Debugging Initials for Doctor ID {doctor.Id}: Name = '{doctor.Name}'");
                    var nameParts = doctor.Name.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var filteredParts = nameParts.Where(s => !string.IsNullOrEmpty(s));

                    if (filteredParts.Any())
                    {
                        doctorInitials = string.Concat(filteredParts.Select(s => s.First())).ToUpper();
                    }
                    else
                    {
                        Debug.WriteLine($"WARNING: After splitting and filtering, no parts found for doctor ID {doctor.Id}, name '{doctor.Name}'. Using default initials 'DR'.");
                    }
                }
                else
                {
                    Debug.WriteLine($"WARNING: Doctor Name is null or whitespace for ID {doctor.Id}. Using default initials 'DR'.");
                }

                string generatedLocation = $"{locationBase} Unit {doctorId % 10 + 1} - Dr. {doctorInitials}";

                doctorSchedule = new DoctorSchedule
                {
                    DoctorId = doctorId,
                    Date = dateToDisplay.Date,
                    StartTime = startTime,
                    EndTime = endTime,
                    LunchStartTime = lunchStartTime,
                    LunchEndTime = lunchEndTime,
                    Location = generatedLocation,
                    IsAvailable = true,
                    MinWorkTime = new TimeSpan(8, 0, 0),
                    MaxWorkTime = new TimeSpan(17, 0, 0)
                };

                _context.DoctorSchedules.Add(doctorSchedule);
                await _context.SaveChangesAsync();
                TempData["InfoMessage"] = $"No specific working schedule was found for {dateToDisplay.ToLongDateString()}. A new default schedule has been generated and saved.";
            }

            var todaysAppointments = await _context.Appointments
                                                .Include(a => a.Patient)
                                                .Where(a => a.DoctorId == doctorId && a.AppointmentDateTime.Date == dateToDisplay.Date)
                                                .OrderBy(a => a.AppointmentDateTime)
                                                .ToListAsync();

            var viewModel = new DoctorScheduleViewModel
            {
                Doctor = doctor,
                DoctorSchedule = doctorSchedule,
                TodaysAppointments = todaysAppointments,
                DailyActivities = new List<string>()
            };

            if (viewModel.DoctorSchedule == null)
            {
                TempData["InfoMessage"] = $"No specific working schedule defined for {dateToDisplay.ToLongDateString()}. Displaying general activities.";
                viewModel.DailyActivities.Add("Review pending lab results");
                viewModel.DailyActivities.Add("Prepare for ward rounds");
                viewModel.DailyActivities.Add("Follow up on patient cases");
            }
            else
            {
                if (!viewModel.DailyActivities.Any())
                {
                    viewModel.DailyActivities.Add("Morning consultations & rounds");
                    viewModel.DailyActivities.Add("Administrative tasks & paperwork");
                    viewModel.DailyActivities.Add("Patient follow-ups");
                }
            }

            return View(viewModel);
        }

        // GET: Doctor/Patients
        [HttpGet]
        public async Task<IActionResult> Patients(string searchString)
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You are not authorized to view patient lists.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == HttpContext.Session.GetString("Username"));
            int doctorId = 0;

            if (currentUser != null && currentUser.DoctorId.HasValue)
            {
                doctorId = currentUser.DoctorId.Value;
            }
            else
            {
                TempData["ErrorMessage"] = "Your doctor profile could not be found or is incomplete. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Associated doctor profile not found in the Doctors table. Please contact support.";
                return RedirectToAction("Login", "Account");
            }

            // Fetch distinct patient IDs associated with this doctor through appointments
            var patientIds = await _context.Appointments
                                           .Where(a => a.DoctorId == doctorId)
                                           .Select(a => a.PatientId)
                                           .Distinct()
                                           .ToListAsync();

            IQueryable<Patient> patientsQuery = _context.Patients
                                                        .Where(p => patientIds.Contains(p.PatientId));

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                patientsQuery = patientsQuery.Where(p => p.Name.Contains(searchString) ||
                                                         p.ContactNumber.Contains(searchString) ||
                                                         (p.Email != null && p.Email.Contains(searchString)));
            }

            var patientsFromDb = await patientsQuery.OrderBy(p => p.Name).ToListAsync();

            // Populate the new PatientDisplayViewModel list
            var patientsForDisplay = new List<PatientDisplayViewModel>();
            foreach (var patient in patientsFromDb)
            {
                // Fetch the most recent appointment for this patient with this specific doctor
                var lastAppointment = await _context.Appointments
                                                    .Where(a => a.DoctorId == doctorId && a.PatientId == patient.PatientId && a.AppointmentDateTime < DateTime.Now)
                                                    .OrderByDescending(a => a.AppointmentDateTime)
                                                    .FirstOrDefaultAsync();

                patientsForDisplay.Add(new PatientDisplayViewModel
                {
                    PatientId = patient.PatientId,
                    Name = patient.Name,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    ContactNumber = patient.ContactNumber,
                    Email = patient.Email,
                    LastAppointmentDateTime = lastAppointment?.AppointmentDateTime // Will be null if no past appointment
                });
            }

            var viewModel = new DoctorPatientsViewModel
            {
                Doctor = doctor,
                Patients = patientsForDisplay // Pass the list of PatientDisplayViewModel
            };

            ViewData["Title"] = $"{doctor.Name}'s Patients";
            ViewData["CurrentFilter"] = searchString;
            return View(viewModel);
        }

        // GET: Doctor/Billing (Example for a doctor to view their billing)
        [HttpGet]
        public IActionResult Billing()
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You are not authorized to view billing information.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "My Billing";
            return View();
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }
    }
}
