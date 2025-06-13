// Controllers/BillController.cs
using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using System.Collections.Generic;
using System;
using System.Diagnostics; // For Debug.WriteLine

namespace HospitalManagementSystem.Controllers
{
    public class BillController : Controller
    {
        private readonly HospitalDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BillController(HospitalDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(_httpContextAccessor.HttpContext?.Session.GetString("Username"));
        private bool IsDoctor() => _httpContextAccessor.HttpContext?.Session.GetString("Role") == "Doctor";
        private bool IsAdmin() => _httpContextAccessor.HttpContext?.Session.GetString("Role") == "Admin";
        private bool IsPatient() => _httpContextAccessor.HttpContext?.Session.GetString("Role") == "Patient";

        private int? GetPatientIdFromSession() => _httpContextAccessor.HttpContext?.Session.GetInt32("PatientId");
        private int? GetDoctorIdFromSession() => _httpContextAccessor.HttpContext?.Session.GetInt32("DoctorId");

        // GET: Bill/Index (For Admin/Doctor to view all bills or their own)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to view this page.";
                return RedirectToAction("Login", "Account");
            }

            IQueryable<Bill> bills = _context.Bills
                                            .Include(b => b.Patient)  // Bill's Patient
                                            .Include(b => b.Doctor)   // Bill's Doctor
                                            .Include(b => b.BillItems) // Bill's Items
                                            .Include(b => b.Appointment) // Bill's Appointment
                                                .ThenInclude(a => a.Patient) // From Appointment, load its Patient
                                            .Include(b => b.Appointment) // Start a new chain from Bill to Appointment
                                                .ThenInclude(a => a.Doctor);   // From Appointment, load its Doctor


            if (IsDoctor())
            {
                int? doctorId = GetDoctorIdFromSession();
                if (!doctorId.HasValue || doctorId.Value == 0)
                {
                    TempData["ErrorMessage"] = "Doctor ID not found in session. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }
                // Doctors see bills they created or bills associated with their appointments
                bills = bills.Where(b => b.DoctorId == doctorId.Value || (b.Appointment != null && b.Appointment.DoctorId == doctorId.Value));
                ViewData["Title"] = "My Bills";
            }
            else if (IsAdmin())
            {
                ViewData["Title"] = "All Hospital Bills";
            }

            return View(await bills.OrderByDescending(b => b.BillDate).ToListAsync());
        }

        // GET: Bill/MyBills (For Patient to view their own bills)
        [HttpGet]
        public async Task<IActionResult> MyBills()
        {
            if (!IsLoggedIn() || !IsPatient())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Patient to view your bills.";
                return RedirectToAction("Login", "Account");
            }

            int? patientId = GetPatientIdFromSession();
            if (!patientId.HasValue || patientId.Value == 0)
            {
                TempData["ErrorMessage"] = "Patient ID not found in session. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var patientBills = await _context.Bills
                                            .Include(b => b.Patient)
                                            .Include(b => b.Doctor)
                                            .Include(b => b.BillItems)
                                            .Include(b => b.Appointment)
                                                .ThenInclude(a => a.Patient) // From Appointment, load its Patient
                                            .Include(b => b.Appointment) // Start a new chain from Bill to Appointment
                                                .ThenInclude(a => a.Doctor)   // From Appointment, load its Doctor
                                            .Where(b => b.PatientId == patientId.Value)
                                            .OrderByDescending(b => b.BillDate)
                                            .ToListAsync();

            ViewData["Title"] = "My Bills";
            return View(patientBills);
        }

        // GET: Bill/Create (For Doctor/Admin to create a new bill for an appointment)
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to create bills.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name }).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name }).ToListAsync();

            Appointment selectedAppointment = null;
            if (appointmentId.HasValue && appointmentId.Value > 0)
            {
                selectedAppointment = await _context.Appointments
                                                    .Include(a => a.Patient)
                                                    .Include(a => a.Doctor)
                                                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);

                if (selectedAppointment == null)
                {
                    TempData["ErrorMessage"] = "Selected appointment not found.";
                    return RedirectToAction("Index"); // Or back to appointment list
                }

                // Doctors can only create bills for their own appointments
                if (IsDoctor() && GetDoctorIdFromSession() != selectedAppointment.DoctorId)
                {
                    TempData["ErrorMessage"] = "Doctors can only create bills for their own appointments.";
                    return RedirectToAction("Index");
                }

                // Check if a bill already exists for this appointment
                bool billExists = await _context.Bills.AnyAsync(b => b.AppointmentId == appointmentId.Value);
                if (billExists)
                {
                    TempData["InfoMessage"] = $"A bill already exists for Appointment ID {appointmentId.Value}. Please edit the existing bill instead.";
                    return RedirectToAction("Details", new { id = _context.Bills.Where(b => b.AppointmentId == appointmentId.Value).Select(b => b.BillId).FirstOrDefault() });
                }

                ViewBag.SelectedAppointment = selectedAppointment;
            }
            else
            {
                // If no appointmentId is provided, list all active appointments
                ViewBag.Appointments = await _context.Appointments
                                                     .Include(a => a.Patient)
                                                     .Include(a => a.Doctor)
                                                     .Where(a => a.Status == "Completed" || a.Status == "Scheduled") // Only show completed or scheduled appointments
                                                     .Where(a => !_context.Bills.Any(b => b.AppointmentId == a.Id)) // Only appointments without existing bills
                                                     .Select(a => new { Value = a.Id, Text = $"{a.AppointmentDateTime:yyyy-MM-dd hh:mm tt} - {a.Patient.Name} with {a.Doctor.Name}" })
                                                     .ToListAsync();
            }

            var bill = new Bill
            {
                AppointmentId = selectedAppointment?.Id ?? 0,
                PatientId = selectedAppointment?.PatientId ?? 0,
                DoctorId = GetDoctorIdFromSession() ?? (IsAdmin() ? null : (int?)0),
                BillDate = DateTime.Today,
                Status = "Pending",
                BillItems = new List<BillItem>() // Initialize with an empty list for the form
            };

            ViewData["Title"] = "Create New Bill";
            return View(bill);
        }

        // POST: Bill/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bill bill, [FromForm] string[] itemNames, [FromForm] decimal[] quantities, [FromForm] decimal[] unitPrices)
        {
            // Debugging: Log incoming form data
            Debug.WriteLine($"Bill POST: AppointmentId={bill.AppointmentId}, PatientId={bill.PatientId}, DoctorId={bill.DoctorId}");
            Debug.WriteLine($"Bill Items received: Names={itemNames?.Length}, Quantities={quantities?.Length}, UnitPrices={unitPrices?.Length}");
            if (itemNames != null)
            {
                for (int i = 0; i < itemNames.Length; i++)
                {
                    Debug.WriteLine($"  Item {i}: Name='{itemNames[i]}', Qty={quantities[i]}, Price={unitPrices[i]}");
                }
            }


            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to create bills.";
                return RedirectToAction("Login", "Account");
            }

            // --- IMPORTANT: Load Navigation Properties BEFORE ModelState.IsValid check ---
            // If AppointmentId, PatientId, DoctorId are sent, ensure the corresponding objects are loaded.
            // This is crucial because your Bill model defines these navigation properties as non-nullable.

            if (bill.AppointmentId > 0)
            {
                // Load Appointment with its Patient and Doctor, as Bill needs them.
                bill.Appointment = await _context.Appointments
                                                 .Include(a => a.Patient)
                                                 .Include(a => a.Doctor)
                                                 .FirstOrDefaultAsync(a => a.Id == bill.AppointmentId);
                if (bill.Appointment == null)
                {
                    ModelState.AddModelError("AppointmentId", "Associated appointment not found or invalid.");
                }
                else
                {
                    // Ensure PatientId on bill matches the patient of the appointment
                    // This is a safety check and ensures consistency for the Bill.Patient property
                    bill.PatientId = bill.Appointment.PatientId;
                }
            }
            else
            {
                ModelState.AddModelError("AppointmentId", "An appointment must be selected to create a bill.");
            }

            // Patient property needs to be loaded if it's not already via Appointment
            // This ensures bill.Patient is not null for EF Core
            if (bill.PatientId > 0 && bill.Patient == null) // Check if Patient is already loaded via Appointment
            {
                bill.Patient = await _context.Patients.FindAsync(bill.PatientId);
                if (bill.Patient == null)
                {
                    ModelState.AddModelError("PatientId", "Patient not found or invalid.");
                }
            }


            // Doctor property needs to be loaded if DoctorId is provided
            if (bill.DoctorId.HasValue && bill.DoctorId.Value > 0 && bill.Doctor == null) // Check if Doctor is already loaded via Appointment
            {
                bill.Doctor = await _context.Doctors.FindAsync(bill.DoctorId.Value);
                if (bill.Doctor == null)
                {
                    ModelState.AddModelError("DoctorId", "Doctor not found or invalid.");
                }
            }


            // Authorization checks (should be done after loading appointment details)
            if (IsDoctor() && bill.Appointment != null && GetDoctorIdFromSession() != bill.Appointment.DoctorId)
            {
                ModelState.AddModelError("", "Doctors can only create bills for their own appointments.");
            }


            // Set DoctorId if it's a doctor creating the bill and not explicitly set (e.g. by Admin)
            if (IsDoctor() && !bill.DoctorId.HasValue)
            {
                bill.DoctorId = GetDoctorIdFromSession();
            }
            else if (IsAdmin() && !bill.DoctorId.HasValue && bill.Appointment != null && bill.Appointment.DoctorId != 0)
            {
                bill.DoctorId = bill.Appointment.DoctorId;
            }


            // Manually populate BillItems from form arrays
            bill.BillItems = new List<BillItem>(); // Initialize to ensure it's not null
            decimal totalAmount = 0;

            if (itemNames != null && quantities != null && unitPrices != null &&
                itemNames.Length == quantities.Length && itemNames.Length == unitPrices.Length)
            {
                for (int i = 0; i < itemNames.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(itemNames[i]) && quantities[i] > 0 && unitPrices[i] >= 0)
                    {
                        decimal itemAmount = quantities[i] * unitPrices[i];
                        bill.BillItems.Add(new BillItem
                        {
                            ItemName = itemNames[i].Trim(),
                            Quantity = quantities[i],
                            UnitPrice = unitPrices[i],
                            Amount = itemAmount
                        });
                        totalAmount += itemAmount;
                    }
                    else
                    {
                        ModelState.AddModelError($"itemNames[{i}]", "Item name is required, quantity must be greater than 0, and unit price must be non-negative.");
                        Debug.WriteLine($"Validation error for item {i}: Name='{itemNames[i]}', Qty={quantities[i]}, Price={unitPrices[i]}");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("BillItems", "Invalid bill item data submitted. Please ensure all fields are filled.");
            }


            bill.TotalAmount = totalAmount;
            bill.BillDate = DateTime.Today;
            if (string.IsNullOrEmpty(bill.Status))
            {
                bill.Status = "Pending";
            }

            // Validate that at least one valid bill item was added
            if (!bill.BillItems.Any())
            {
                ModelState.AddModelError("BillItems", "At least one bill item is required to generate a bill.");
                Debug.WriteLine("Validation Error: No valid bill items were processed.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(bill);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bill created successfully!";
                    // Redirect to Bill Index page after successful creation
                    return RedirectToAction("Index", "Bill");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating bill: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    TempData["ErrorMessage"] = $"An error occurred while creating the bill: {ex.Message}. Please check server logs for details.";
                }
            }
            else
            {
                Debug.WriteLine("Model state is invalid after processing BillItems:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Debug.WriteLine($"  {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // If ModelState is invalid or an error occurred, re-populate ViewBags and return to view
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name }).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name }).ToListAsync();

            // Re-populate ViewBag.SelectedAppointment if it was initially provided
            if (bill.AppointmentId > 0 && bill.Appointment != null)
            {
                ViewBag.SelectedAppointment = bill.Appointment;
            }
            else
            {
                ViewBag.Appointments = await _context.Appointments
                                                    .Include(a => a.Patient)
                                                    .Include(a => a.Doctor)
                                                    .Where(a => a.Status == "Completed" || a.Status == "Scheduled")
                                                    .Where(a => !_context.Bills.Any(b => b.AppointmentId == a.Id))
                                                    .Select(a => new { Value = a.Id, Text = $"{a.AppointmentDateTime:yyyy-MM-dd hh:mm tt} - {a.Patient.Name} with {a.Doctor.Name}" })
                                                    .ToListAsync();
            }

            ViewData["Title"] = "Create New Bill";
            return View(bill);
        }

        // GET: Bill/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "You must be logged in to view bill details.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                                     .Include(b => b.Patient)
                                     .Include(b => b.Doctor)
                                     .Include(b => b.BillItems)
                                     .Include(b => b.Appointment)
                                         .ThenInclude(a => a.Patient) // Correct: From Appointment, load its Patient
                                     .Include(b => b.Appointment) // Correct: Start a new chain from Bill to Appointment
                                         .ThenInclude(a => a.Doctor)   // Correct: From Appointment, load its Doctor
                                     .FirstOrDefaultAsync(m => m.BillId == id);

            if (bill == null)
            {
                TempData["ErrorMessage"] = $"Bill with ID {id} not found.";
                return NotFound();
            }

            // Authorization: Admin, or Doctor (for bills they are associated with), or Patient (for their own bills)
            bool isAuthorized = IsAdmin() ||
                               (IsDoctor() && (GetDoctorIdFromSession() == bill.DoctorId || (bill.Appointment != null && GetDoctorIdFromSession() == bill.Appointment.DoctorId))) ||
                               (IsPatient() && GetPatientIdFromSession() == bill.PatientId);

            if (!isAuthorized)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this bill.";
                if (IsPatient()) return RedirectToAction("MyBills", "Bill");
                if (IsDoctor()) return RedirectToAction("Index", "Bill");
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = $"Bill Details - {bill.BillId}";
            return View(bill);
        }


        // GET: Bill/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit bills.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var bill = await _context.Bills
                                    .Include(b => b.Patient)
                                    .Include(b => b.Doctor)
                                    .Include(b => b.BillItems)
                                    .Include(b => b.Appointment)
                                        .ThenInclude(a => a.Patient) // Correct: From Appointment, load its Patient
                                    .Include(b => b.Appointment) // Correct: Start a new chain from Bill to Appointment
                                        .ThenInclude(a => a.Doctor)   // Correct: From Appointment, load its Doctor
                                    .FirstOrDefaultAsync(m => m.BillId == id);

            if (bill == null)
            {
                TempData["ErrorMessage"] = $"Bill with ID {id} not found.";
                return NotFound();
            }

            // Authorization: Admin, or Doctor (for bills they are associated with)
            if (!(IsAdmin() || (IsDoctor() && (GetDoctorIdFromSession() == bill.DoctorId || (bill.Appointment != null && GetDoctorIdFromSession() == bill.Appointment.DoctorId)))))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this bill.";
                if (IsDoctor()) return RedirectToAction("Index", "Bill");
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name }).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name }).ToListAsync();
            ViewBag.SelectedAppointment = bill.Appointment; // Pass the associated appointment

            ViewData["Title"] = "Edit Bill";
            return View(bill);
        }

        // POST: Bill/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bill bill, [FromForm] string[] itemNames, [FromForm] decimal[] quantities, [FromForm] decimal[] unitPrices, [FromForm] int[] billItemIds)
        {
            // Debugging: Log incoming form data for Edit
            Debug.WriteLine($"Bill Edit POST: BillId={id}, AppointmentId={bill.AppointmentId}, PatientId={bill.PatientId}, DoctorId={bill.DoctorId}");
            Debug.WriteLine($"Bill Items received: Names={itemNames?.Length}, Quantities={quantities?.Length}, UnitPrices={unitPrices?.Length}, ItemIds={billItemIds?.Length}");
            if (itemNames != null)
            {
                for (int i = 0; i < itemNames.Length; i++)
                {
                    Debug.WriteLine($"  Item {i}: Id={billItemIds[i]}, Name='{itemNames[i]}', Qty={quantities[i]}, Price={unitPrices[i]}");
                }
            }

            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit bills.";
                return RedirectToAction("Login", "Account");
            }

            if (id != bill.BillId) return NotFound();

            var existingBill = await _context.Bills
                                            .Include(b => b.BillItems) // Include existing items to manage them
                                            .Include(b => b.Appointment)
                                                .ThenInclude(a => a.Patient) // Include Appointment's Patient
                                            .Include(b => b.Appointment) // Start a new chain from Bill to Appointment
                                                .ThenInclude(a => a.Doctor)   // Include Appointment's Doctor
                                            .FirstOrDefaultAsync(b => b.BillId == id);

            if (existingBill == null)
            {
                TempData["ErrorMessage"] = "The bill you are trying to edit was not found or has been deleted.";
                return NotFound();
            }

            // Authorization check
            if (!(IsAdmin() || (IsDoctor() && (GetDoctorIdFromSession() == existingBill.DoctorId || (existingBill.Appointment != null && GetDoctorIdFromSession() == existingBill.Appointment.DoctorId)))))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this bill.";
                if (IsDoctor()) return RedirectToAction("Index", "Bill");
                return RedirectToAction("Login", "Account");
            }

            // Update main bill properties
            existingBill.BillDate = bill.BillDate;
            existingBill.Status = bill.Status;
            existingBill.Notes = bill.Notes;

            // DoctorId update logic
            if (IsAdmin())
            {
                existingBill.DoctorId = bill.DoctorId;
            }
            else if (IsDoctor())
            {
                existingBill.DoctorId = GetDoctorIdFromSession();
            }


            // Handle BillItems: Delete old, add new, update existing
            if (existingBill.BillItems == null)
            {
                existingBill.BillItems = new List<BillItem>();
            }

            var currentItemIds = new HashSet<int>(existingBill.BillItems.Select(bi => bi.BillItemId));
            var incomingItemIds = new HashSet<int>(billItemIds?.Where(x => x != 0) ?? Enumerable.Empty<int>());

            // Remove items that are no longer in the form
            foreach (var existingItem in existingBill.BillItems.ToList())
            {
                if (!incomingItemIds.Contains(existingItem.BillItemId))
                {
                    _context.BillItems.Remove(existingItem);
                }
            }

            decimal totalAmount = 0;
            if (itemNames != null && quantities != null && unitPrices != null && billItemIds != null &&
                itemNames.Length == quantities.Length && itemNames.Length == unitPrices.Length && itemNames.Length == billItemIds.Length)
            {
                for (int i = 0; i < itemNames.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(itemNames[i]) && quantities[i] > 0 && unitPrices[i] >= 0)
                    {
                        decimal itemAmount = quantities[i] * unitPrices[i];
                        if (billItemIds[i] != 0 && currentItemIds.Contains(billItemIds[i]))
                        {
                            // Update existing item
                            var itemToUpdate = existingBill.BillItems.FirstOrDefault(bi => bi.BillItemId == billItemIds[i]);
                            if (itemToUpdate != null)
                            {
                                itemToUpdate.ItemName = itemNames[i].Trim();
                                itemToUpdate.Quantity = quantities[i];
                                itemToUpdate.UnitPrice = unitPrices[i];
                                itemToUpdate.Amount = itemAmount;
                                _context.Entry(itemToUpdate).State = EntityState.Modified;
                            }
                        }
                        else // billItemIds[i] is 0 or not found in current items (meaning it's a new item)
                        {
                            // Add new item
                            existingBill.BillItems.Add(new BillItem
                            {
                                ItemName = itemNames[i].Trim(),
                                Quantity = quantities[i],
                                UnitPrice = unitPrices[i],
                                Amount = itemAmount
                            });
                        }
                        totalAmount += itemAmount;
                    }
                    else
                    {
                        ModelState.AddModelError($"itemNames[{i}]", "Item name is required, quantity must be greater than 0, and unit price must be non-negative.");
                        Debug.WriteLine($"Validation error for item {i}: Id={billItemIds[i]}, Name='{itemNames[i]}', Qty={quantities[i]}, Price={unitPrices[i]}");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("BillItems", "Invalid bill item data submitted. Please ensure all fields are filled and consistent.");
            }

            existingBill.TotalAmount = totalAmount;

            // Validate that at least one valid bill item was added
            if (!existingBill.BillItems.Any())
            {
                ModelState.AddModelError("BillItems", "At least one bill item is required to generate a bill.");
                Debug.WriteLine("Validation Error: No valid bill items were processed after Edit.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(existingBill); // Use Update for the main Bill entity
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bill updated successfully!";
                    return RedirectToAction("Details", new { id = existingBill.BillId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BillExists(existingBill.BillId))
                    {
                        TempData["ErrorMessage"] = "The bill was deleted by another user. Cannot save changes.";
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating bill: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    TempData["ErrorMessage"] = $"An error occurred while updating the bill: {ex.Message}. Please check server logs for details.";
                }
            }
            else
            {
                Debug.WriteLine("Model state is invalid after processing BillItems for Edit:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Debug.WriteLine($"  {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // If ModelState is invalid or an error occurred, re-populate ViewBags and return to view
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.Name).Select(p => new { Value = p.PatientId, Text = p.Name }).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).Select(d => new { Value = d.Id, Text = d.Name }).ToListAsync();
            ViewBag.SelectedAppointment = existingBill.Appointment;
            ViewData["Title"] = "Edit Bill";
            return View(existingBill);
        }

        // POST: Bill/MarkAsPaid/5 (AJAX endpoint)
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            if (!IsLoggedIn() || (!IsAdmin() && !IsDoctor()))
            {
                return Json(new { success = false, message = "Unauthorized to mark bills as paid." });
            }

            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
            {
                return Json(new { success = false, message = "Bill not found." });
            }

            // Doctor can only mark their own bills as paid
            if (IsDoctor() && (GetDoctorIdFromSession() != bill.DoctorId && (bill.Appointment != null && GetDoctorIdFromSession() != bill.Appointment.DoctorId)))
            {
                return Json(new { success = false, message = "Doctors can only mark their associated bills as paid." });
            }

            if (bill.Status == "Paid")
            {
                return Json(new { success = false, message = "Bill is already paid." });
            }

            bill.Status = "Paid";
            try
            {
                _context.Update(bill);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Bill marked as paid successfully." });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error marking bill as paid: {ex.Message}");
                return Json(new { success = false, message = $"Error marking bill as paid: {ex.Message}" });
            }
        }

        // POST: Bill/Delete/5 (AJAX endpoint, only for Admin)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn() || !IsAdmin()) // Only admin can delete bills
            {
                return Json(new { success = false, message = "Unauthorized to delete bills." });
            }

            var bill = await _context.Bills.Include(b => b.BillItems).FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null)
            {
                return Json(new { success = false, message = "Bill not found." });
            }

            try
            {
                _context.BillItems.RemoveRange(bill.BillItems); // Remove child items first
                _context.Bills.Remove(bill);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Bill deleted successfully!" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting bill: {ex.Message}");
                return Json(new { success = false, message = $"Error deleting bill: {ex.Message}" });
            }
        }

        private bool BillExists(int id)
        {
            return _context.Bills.Any(e => e.BillId == id);
        }
    }
}
