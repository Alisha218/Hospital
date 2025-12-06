using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Controllers
{
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string searchString, string bloodGroup)
        {
            var patients = from p in _context.Patients select p;

            // Search logic
            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p =>
                    p.first_name.Contains(searchString) ||
                    p.last_name.Contains(searchString) ||
                    p.contact_number.Contains(searchString) ||
                    p.email.Contains(searchString));
            }

            // Filter by blood group
            if (!string.IsNullOrEmpty(bloodGroup))
            {
                patients = patients.Where(p => p.blood_group == bloodGroup);
            }

            // Prepare blood groups for dropdown
            var bloodGroups = await _context.Patients
                .Select(p => p.blood_group)
                .Distinct()
                .Where(b => !string.IsNullOrEmpty(b))
                .ToListAsync();

            ViewBag.BloodGroups = new SelectList(bloodGroups);
            ViewBag.CurrentFilter = searchString;

            return View(await patients.ToListAsync());
        }

        // GET: Patients/Dashboard/5
        public async Task<IActionResult> Dashboard(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.patient_id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Calculate statistics
            var totalAppointments = patient.Appointments?.Count ?? 0;
            var upcomingAppointments = patient.Appointments?
                .Where(a => a.appointment_date > DateTime.Now && a.status != "Cancelled")
                .Count() ?? 0;
            var totalBilled = patient.Appointments?
                .Where(a => a.Billing != null)
                .Sum(a => a.Billing.total_amount) ?? 0;

            ViewBag.TotalAppointments = totalAppointments;
            ViewBag.UpcomingAppointments = upcomingAppointments;
            ViewBag.TotalBilled = totalBilled;

            return View(patient);
        }

        // GET: Patients/MedicalHistory/5
        public async Task<IActionResult> MedicalHistory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecords = await _context.MedicalRecords
                .Where(mr => mr.Appointment.patient_id == id)
                .Include(mr => mr.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(mr => mr.Prescriptions)
                .Include(mr => mr.TestResults)
                .OrderByDescending(mr => mr.visit_date)
                .ToListAsync();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            ViewBag.PatientName = $"{patient.first_name} {patient.last_name}";
            return View(medicalRecords);
        }

        // GET: Patients/BillingHistory/5
        public async Task<IActionResult> BillingHistory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var billings = await _context.Billings
                .Where(b => b.Appointment.patient_id == id)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .OrderByDescending(b => b.Appointment.appointment_date)
                .ToListAsync();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            ViewBag.PatientName = $"{patient.first_name} {patient.last_name}";
            ViewBag.TotalPaid = billings.Where(b => b.payment_status == "Paid").Sum(b => b.total_amount);
            ViewBag.TotalPending = billings.Where(b => b.payment_status != "Paid").Sum(b => b.total_amount);

            return View(billings);
        }

        // POST: Patients/SendAppointmentReminder
        [HttpPost]
        public async Task<IActionResult> SendAppointmentReminder(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound();
            }

            // Get upcoming appointments
            var upcomingAppointments = await _context.Appointments
                .Where(a => a.patient_id == patientId &&
                           a.appointment_date > DateTime.Now &&
                           a.status != "Cancelled")
                .Include(a => a.Doctor)
                .ToListAsync();

            // Business Logic: Send reminder (simulated)
            foreach (var appointment in upcomingAppointments)
            {
                // In real application, send email/SMS here
                // For now, just log it
                Console.WriteLine($"Reminder sent to {patient.email}: " +
                    $"Appointment with Dr. {appointment.Doctor.first_name} " +
                    $"on {appointment.appointment_date:MM/dd/yyyy HH:mm}");
            }

            TempData["Message"] = $"Reminders sent for {upcomingAppointments.Count} upcoming appointments.";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // GET: Patients/GenerateReport/5
        public async Task<IActionResult> GenerateReport(int? id, string reportType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.patient_id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Generate different report types
            switch (reportType?.ToLower())
            {
                case "medical":
                    return View("MedicalReport", patient);
                case "billing":
                    return View("BillingReport", patient);
                case "summary":
                default:
                    return View("PatientSummaryReport", patient);
            }
        }

        // Existing CRUD methods (Create, Edit, Delete, Details) remain the same
        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.patient_id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Calculate statistics for the view
            ViewBag.TotalAppointments = patient.Appointments?.Count ?? 0;
            ViewBag.PendingAppointments = patient.Appointments?
                .Where(a => a.status == "Pending").Count() ?? 0;
            ViewBag.TotalBillAmount = patient.Appointments?
                .Where(a => a.Billing != null)
                .Sum(a => a.Billing.total_amount) ?? 0;

            return View(patient);
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("patient_id,first_name,last_name,gender,date_of_birth,contact_number,email,address,blood_group,password")] Patient patient)
        {
            // Business Logic: Validate email uniqueness
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.email == patient.email);

            if (existingPatient != null)
            {
                ModelState.AddModelError("email", "Email already exists in the system.");
            }

            var oneYearAgo = DateTime.Today.AddYears(-1);

            // Check if patient is at least 1 year old
            if (patient.date_of_birth > oneYearAgo)  // Direct comparison, no .HasValue or .Value
            {
                ModelState.AddModelError("date_of_birth", "Patient must be at least 1 year old.");
            }

            // Check if date is not in future
            if (patient.date_of_birth > DateTime.Today)  // Direct comparison
            {
                ModelState.AddModelError("date_of_birth", "Date of birth cannot be in the future.");
            }

            // Optional: Check if patient is not too old (e.g., 150 years)
            var maxAge = DateTime.Today.AddYears(-150);
            if (patient.date_of_birth < maxAge)
            {
                ModelState.AddModelError("date_of_birth", "Date of birth seems unrealistic.");
            }

            if (ModelState.IsValid)
            {
                // Business Logic: Generate patient ID if not provided
                if (patient.patient_id == 0)
                {
                    // Get the last patient ID and increment
                    var lastPatient = await _context.Patients
                        .OrderByDescending(p => p.patient_id)
                        .FirstOrDefaultAsync();
                    patient.patient_id = lastPatient?.patient_id + 1 ?? 1001;
                }

                // Business Logic: Set registration date
                // patient.registration_date = DateTime.Now;

                _context.Add(patient);
                await _context.SaveChangesAsync();

                // Business Logic: Send welcome email/notification
                // SendWelcomeEmail(patient);

                TempData["SuccessMessage"] = "Patient registered successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("patient_id,first_name,last_name,gender,date_of_birth,contact_number,email,address,blood_group,password")] Patient patient)
        {
            if (id != patient.patient_id)
            {
                return NotFound();
            }

            // Business Logic: Check if email is being changed and validate uniqueness
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.email == patient.email && p.patient_id != id);

            if (existingPatient != null)
            {
                ModelState.AddModelError("email", "Email already exists in the system.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Business Logic: Track modification
                    // patient.modified_date = DateTime.Now;

                    _context.Update(patient);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Patient information updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.patient_id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.patient_id == id);

            if (patient == null)
            {
                return NotFound();
            }

            // Business Logic: Check if patient has active appointments
            var activeAppointments = patient.Appointments?
                .Where(a => a.status != "Cancelled" && a.status != "Completed")
                .ToList();

            if (activeAppointments?.Any() == true)
            {
                TempData["ErrorMessage"] = "Cannot delete patient with active appointments. Please cancel appointments first.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            // Business Logic: Archive instead of delete
            // In production, you might want to soft delete
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Patient deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.patient_id == id);
        }
    }
}