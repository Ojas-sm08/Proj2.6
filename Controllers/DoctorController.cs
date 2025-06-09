using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System; // For DateTime
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // For List<string>
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using System.Diagnostics;

// Added QuestPDF using directives (if you added them for other features)
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.IO;


namespace HospitalManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly HospitalDbContext _context;
        private static readonly Random random = new Random(); // Static random instance for schedule generation

        public DoctorController(HospitalDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        private bool IsDoctor() => HttpContext.Session.GetString("Role") == "Doctor";

        // Helper to generate a random DoctorSchedule for a given doctor and date
        private DoctorSchedule GenerateRandomDoctorSchedule(int doctorId, DateTime date)
        {
            Debug.WriteLine($"Generating random schedule for Doctor ID: {doctorId}, Date: {date.ToShortDateString()}");

            TimeSpan earliestStart = new TimeSpan(8, 0, 0); // 8:00 AM
            TimeSpan latestStart = new TimeSpan(10, 0, 0); // 10:00 AM

            TimeSpan earliestEnd = new TimeSpan(16, 0, 0); // 4:00 PM
            TimeSpan latestEnd = new TimeSpan(18, 0, 0); // 6:00 PM

            TimeSpan minWorkDuration = TimeSpan.FromHours(4); // Minimum 4 hours of work

            // Generate random start time
            // Ensure minutes are multiples of 15 for cleaner slots
            int startHour = random.Next(earliestStart.Hours, latestStart.Hours + 1);
            int startMinute = random.Next(0, 4) * 15; // 0, 15, 30, 45
            TimeSpan startTime = new TimeSpan(startHour, startMinute, 0);

            // Generate random end time, ensuring it's at least minWorkDuration after startTime
            TimeSpan proposedEndTime = startTime.Add(TimeSpan.FromHours(random.Next((int)minWorkDuration.TotalHours, 8))); // 4 to 7 hours after start
            if (proposedEndTime > latestEnd) proposedEndTime = latestEnd;

            // Ensure end time minutes are multiples of 15
            int endHour = proposedEndTime.Hours;
            int endMinute = (int)(Math.Round(proposedEndTime.Minutes / 15.0) * 15);
            TimeSpan endTime = new TimeSpan(endHour, endMinute, 0);

            // If generated endTime is somehow before startTime, or too close, adjust
            if (endTime <= startTime.Add(TimeSpan.FromMinutes(30))) // Ensure at least one 30-min slot
            {
                endTime = startTime.Add(TimeSpan.FromHours(random.Next(2, 5))); // Force minimum 2-5 hours of work
                if (endTime > latestEnd) endTime = latestEnd;
            }


            // Generate random lunch break within working hours
            TimeSpan lunchStartTime = new TimeSpan(12, 0, 0); // Default around noon
            TimeSpan lunchEndTime = new TimeSpan(13, 0, 0); // Default 1 hour lunch

            // Make lunch more dynamic: between 12 PM and 2 PM for start
            int potentialLunchStartHour = random.Next(12, 14); // 12 PM or 1 PM
            int potentialLunchStartMinute = random.Next(0, 4) * 15; // 0, 15, 30, 45
            TimeSpan newLunchStartTime = new TimeSpan(potentialLunchStartHour, potentialLunchStartMinute, 0);

            // Ensure lunch is within working hours
            if (newLunchStartTime < startTime.Add(TimeSpan.FromHours(1))) // Must start at least 1 hour after work begins
            {
                newLunchStartTime = startTime.Add(TimeSpan.FromHours(1));
            }
            if (newLunchStartTime > endTime.Subtract(TimeSpan.FromHours(1.5))) // Must end at least 1.5 hours before work ends
            {
                newLunchStartTime = endTime.Subtract(TimeSpan.FromHours(random.Next(2, 3))); // Start 2-3 hours before end
                newLunchStartTime = new TimeSpan(newLunchStartTime.Hours, (int)(Math.Round(newLunchStartTime.Minutes / 15.0) * 15), 0);
            }

            int lunchDurationMinutes = random.Next(30, 91); // 30 mins to 90 mins (1 hour 30 mins)
            TimeSpan newLunchEndTime = newLunchStartTime.Add(TimeSpan.FromMinutes(lunchDurationMinutes));

            // Final check to ensure lunch is within the overall work time
            if (newLunchEndTime > endTime) newLunchEndTime = endTime;

            Debug.WriteLine($"Generated: Start={startTime}, End={endTime}, Lunch={newLunchStartTime}-{newLunchEndTime}");

            return new DoctorSchedule
            {
                DoctorId = doctorId,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                Location = "Dynamically Assigned Clinic", // Default location for generated schedules
                MinWorkTime = earliestStart, // These are for the *bounds* of generation, not the generated times
                MaxWorkTime = latestEnd,
                LunchStartTime = newLunchStartTime,
                LunchEndTime = newLunchEndTime
            };
        }

        // Helper method to generate random daily activities (non-medical terms)
        private List<string> GenerateDailyActivities(TimeSpan startTime, TimeSpan endTime, TimeSpan lunchStart, TimeSpan lunchEnd)
        {
            var activities = new List<string>();
            var currentTime = startTime;

            // Define some non-medical, hospital-related activities
            string[] morningActivities = {
                "Morning Team Briefing",
                "Reviewing Patient Charts",
                "Ward Rounds (Ground Floor)",
                "Consulting with Specialists"
            };
            string[] midDayActivities = {
                "Responding to Urgent Pagers",
                "Lab Results Review Session",
                "Ward Rounds (Upper Floors)",
                "Administrative Tasks"
            };
            string[] afternoonActivities = {
                "Preparing for Tomorrow's Cases",
                "Follow-up Calls",
                "Discharge Planning Meeting",
                "Paperwork & Documentation"
            };

            // Iterate through the day, adding activities
            while (currentTime < endTime)
            {
                // Skip lunch break
                if (currentTime >= lunchStart && currentTime < lunchEnd)
                {
                    currentTime = lunchEnd; // Jump past lunch
                    if (currentTime >= endTime) break; // If lunch extends past end time, exit loop
                }

                // Add an activity based on the time of day
                // Ensure activity timing doesn't go past end time or during lunch
                if (currentTime >= startTime && currentTime < new TimeSpan(12, 0, 0) && currentTime < lunchStart) // Morning (before lunch)
                {
                    // FIX: Convert TimeSpan to DateTime for 'tt' formatting
                    activities.Add($"({new DateTime().Add(currentTime).ToString(@"hh\:mm tt")}) - " + morningActivities[random.Next(morningActivities.Length)]);
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(random.Next(45, 90))); // Activity duration
                }
                else if (currentTime >= new TimeSpan(12, 0, 0) && currentTime < new TimeSpan(15, 0, 0) && (currentTime < lunchStart || currentTime >= lunchEnd)) // Mid-day (avoiding lunch)
                {
                    // FIX: Convert TimeSpan to DateTime for 'tt' formatting
                    activities.Add($"({new DateTime().Add(currentTime).ToString(@"hh\:mm tt")}) - " + midDayActivities[random.Next(midDayActivities.Length)]);
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(random.Next(45, 90)));
                }
                else if (currentTime >= new TimeSpan(15, 0, 0) && currentTime < endTime && (currentTime < lunchStart || currentTime >= lunchEnd)) // Afternoon (avoiding lunch)
                {
                    // FIX: Convert TimeSpan to DateTime for 'tt' formatting
                    activities.Add($"({new DateTime().Add(currentTime).ToString(@"hh\:mm tt")}) - " + afternoonActivities[random.Next(afternoonActivities.Length)]);
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(random.Next(45, 90)));
                }
                else
                {
                    // Fallback to avoid infinite loops if time doesn't advance, jump forward
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                }
            }

            // Add a "Wrap-up" at the end if the schedule allows and if activities were added
            if (activities.Any() && endTime > new TimeSpan(17, 0, 0) && currentTime < endTime.Subtract(TimeSpan.FromMinutes(30)))
            {
                // FIX: Convert TimeSpan to DateTime for 'tt' formatting
                activities.Add($"({new DateTime().Add(endTime.Subtract(TimeSpan.FromMinutes(30))).ToString(@"hh\:mm tt")}) - Final Wrap-up & Sign-offs");
            }


            return activities;
        }


        // GET: /Doctor/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            Debug.WriteLine("DoctorController.Dashboard: Entering Dashboard action.");

            Debug.WriteLine("--- Current Session Contents in DoctorController.Dashboard ---");
            foreach (var key in HttpContext.Session.Keys)
            {
                Debug.WriteLine($"Session Key: '{key}' | Value: '{HttpContext.Session.GetString(key)}'");
            }
            Debug.WriteLine("-------------------------------------------------------------");


            if (!IsLoggedIn() || !IsDoctor())
            {
                Debug.WriteLine("DoctorController.Dashboard: User not logged in or not a Doctor. Redirecting to Login.");
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view this page.";
                return RedirectToAction("Login", "Account");
            }

            string? doctorIdString = HttpContext.Session.GetString("DoctorId");
            int? doctorId = HttpContext.Session.GetInt32("DoctorId");

            Debug.WriteLine($"DoctorController.Dashboard: Retrieved 'DoctorId' (string): '{doctorIdString ?? "null"}'");
            Debug.WriteLine($"DoctorController.Dashboard: Retrieved 'DoctorId' (int?): '{doctorId?.ToString() ?? "null"}'");


            if (!doctorId.HasValue || doctorId.Value == 0)
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session for schedule. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            Debug.WriteLine($"DoctorController.Dashboard: DoctorId (int?) found in session: {doctorId.Value}. Proceeding to load dashboard.");

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor profile not found for schedule.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = $"Dr. {doctor.Name}'s Dashboard";
            return View(doctor);
        }

        // GET: Doctor/Index (Public Doctor Directory)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors.ToListAsync();
            ViewData["Title"] = "Doctor Directory";
            return View(doctors);
        }

        // GET: Doctor/Manage (Admin Doctor Management) - Renders VIEWS/PATIENT/DOCTORLIST.CSHTML (as previously requested)
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to view this page.";
                return RedirectToAction("Login", "Account");
            }
            var doctors = await _context.Doctors.ToListAsync();
            ViewData["Title"] = "Manage Doctors";
            // Explicitly points to the DoctorList.cshtml in the Patient folder
            return View("~/Views/Patient/DoctorList.cshtml", doctors);
        }

        // GET: Doctor/Create - REMAINS IN DOCTOR FOLDER (Views/Doctor/Create.cshtml)
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to add doctors.";
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Add New Doctor";
            return View();
        }

        // POST: Doctor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to add doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Dr. {doctor.Name} added successfully!";

                var newDoctorUsername = $"dr.{doctor.Name.Replace(" ", "").ToLower()}";
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == newDoctorUsername);

                if (existingUser == null)
                {
                    var newUser = new User
                    {
                        Username = newDoctorUsername,
                        PasswordHash = newDoctorUsername, // HASH THIS IN PRODUCTION!
                        Role = "Doctor"
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] += $" Login account '{newDoctorUsername}' created.";
                }
                else
                {
                    TempData["ErrorMessage"] += $" Warning: Login account '{newDoctorUsername}' already exists.";
                }

                return RedirectToAction(nameof(Manage));
            }
            ViewData["Title"] = "Add New Doctor";
            return View(doctor);
        }


        // GET: Doctor/Edit/5 - Renders Views/Doctor/Edit.cshtml (Implicitly)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to edit doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            ViewData["Title"] = "Edit Doctor";
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Specialization,Description,Contact,Location")] Doctor doctor)
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to edit doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Dr. {doctor.Name} updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Manage));
            }
            ViewData["Title"] = "Edit Doctor";
            return View(doctor);
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        // GET: Doctor/Details/5 - Renders Views/Doctor/Details.cshtml (Implicitly)
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();
            ViewData["Title"] = "Doctor Details";
            return View(doctor);
        }

        // GET: Doctor/Delete/5 - Renders Views/Doctor/Delete.cshtml (Implicitly)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                                        .Include(d => d.Appointments)
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            if (doctor.Appointments != null && doctor.Appointments.Any())
            {
                ViewBag.CanDelete = false;
                TempData["ErrorMessage"] = $"Cannot delete doctor '{doctor.Name}' because they have {doctor.Appointments.Count} existing appointments. Please delete or reassign their appointments first.";
            }
            else
            {
                ViewBag.CanDelete = true;
            }

            ViewData["Title"] = "Delete Doctor";
            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn() || HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to delete doctors.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.Include(d => d.Appointments).FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null)
            {
                return NotFound();
            }

            if (doctor.Appointments != null && doctor.Appointments.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete doctor '{doctor.Name}' because they still have existing appointments. Please delete or reassign their appointments first.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            try
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Dr. {doctor.Name} deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"DbUpdateException during Doctor Delete: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "An error occurred while deleting the doctor. Please try again.";
            }
            return RedirectToAction(nameof(Manage));
        }

        // GET: Doctor/Schedule - Displays the doctor's schedule and appointments
        [HttpGet]
        public async Task<IActionResult> Schedule(DateTime? selectedDate) // NOW ACCEPTS OPTIONAL SELECTED DATE
        {
            if (!IsLoggedIn() || !IsDoctor())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Doctor to view your schedule.";
                return RedirectToAction("Login", "Account");
            }

            int? doctorId = HttpContext.Session.GetInt32("DoctorId");

            if (!doctorId.HasValue || doctorId.Value == 0)
            {
                TempData["ErrorMessage"] = "Doctor ID not found in session for schedule. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor profile not found for schedule.";
                return RedirectToAction("Login", "Account");
            }

            // If no date is selected, default to today
            DateTime displayDate = selectedDate?.Date ?? DateTime.Today;

            // Pass the selected date to the view to pre-fill the date picker
            ViewData["SelectedDate"] = displayDate.ToString("yyyy-MM-dd");


            var doctorSchedule = await _context.DoctorSchedules
                                               .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId.Value && ds.Date.Date == displayDate.Date);

            // Always ensure a doctorSchedule exists. If not found, generate and save it.
            if (doctorSchedule == null)
            {
                doctorSchedule = GenerateRandomDoctorSchedule(doctorId.Value, displayDate.Date);
                _context.DoctorSchedules.Add(doctorSchedule);
                await _context.SaveChangesAsync();
                TempData["InfoMessage"] = $"A new schedule has been generated for {displayDate.ToShortDateString()}.";
            }

            // Generate daily activities based on the generated/found schedule times
            var dailyActivities = GenerateDailyActivities(doctorSchedule.StartTime, doctorSchedule.EndTime, doctorSchedule.LunchStartTime, doctorSchedule.LunchEndTime);

            var todaysAppointments = await _context.Appointments
                                                   .Include(a => a.Patient)
                                                   .Where(a => a.DoctorId == doctorId.Value
                                                            && a.AppointmentDateTime.Date == displayDate.Date
                                                            && a.Status != "Completed") // NEW: Filter out completed appointments
                                                   .OrderBy(a => a.AppointmentDateTime)
                                                   .ToListAsync();

            var viewModel = new DoctorScheduleViewModel
            {
                Doctor = doctor,
                DoctorSchedule = doctorSchedule,
                DailyActivities = dailyActivities,
                TodaysAppointments = todaysAppointments
            };

            ViewData["Title"] = $"{doctor.Name}'s Daily Schedule - {displayDate.ToShortDateString()}"; // Update title with selected date
            return View(viewModel);
        }
    }
}
