using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Department)
                .ToListAsync();
            return View(doctors);
        }

        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.DoctorSchedules)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                        .ThenInclude(mr => mr.Prescriptions)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                        .ThenInclude(mr => mr.TestResults)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.doctor_id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Calculate statistics
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var todayAppointments = doctor.Appointments?
                .Where(a => a.appointment_date.Date == today)  // REMOVED .HasValue
                .Count() ?? 0;

            var monthAppointments = doctor.Appointments?
                .Where(a => a.appointment_date >= monthStart && a.appointment_date <= monthEnd)  // REMOVED .HasValue
                .Count() ?? 0;

            var completedAppointments = doctor.Appointments?
                .Where(a => a.status == "Completed")
                .Count() ?? 0;

            var totalPatients = doctor.Appointments?
                .Select(a => a.patient_id)
                .Distinct()
                .Count() ?? 0;

            ViewBag.TodayAppointments = todayAppointments;
            ViewBag.MonthAppointments = monthAppointments;
            ViewBag.CompletedAppointments = completedAppointments;
            ViewBag.TotalPatients = totalPatients;
            ViewBag.Today = today.ToString("MMMM dd, yyyy");

            // Get upcoming appointments
            var upcomingAppointments = doctor.Appointments?
                .Where(a => a.appointment_date > DateTime.Now && a.status != "Cancelled")  // REMOVED .HasValue
                .OrderBy(a => a.appointment_date)
                .Take(5)
                .ToList() ?? new List<Appointment>();

            ViewBag.UpcomingAppointments = upcomingAppointments;

            return View(doctor);
        }

        // GET: Doctors/Create
        public IActionResult Create()
        {
            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name");
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("doctor_id,first_name,last_name,specialization,contact_number,email,status,department_id")] Doctor doctor)
        {
            // Business Logic: Validate email uniqueness
            var existingDoctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.email == doctor.email);

            if (existingDoctor != null)
            {
                ModelState.AddModelError("email", "Email already exists in the system.");
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(doctor.status))
                {
                    doctor.status = "Active";
                }

                doctor.first_name = CapitalizeFirstLetter(doctor.first_name?.Trim());
                doctor.last_name = CapitalizeFirstLetter(doctor.last_name?.Trim());
                doctor.specialization = CapitalizeFirstLetter(doctor.specialization?.Trim());

                _context.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor added successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name", doctor.department_id);
            return View(doctor);
        }

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name", doctor.department_id);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("doctor_id,first_name,last_name,specialization,contact_number,email,status,department_id")] Doctor doctor)
        {
            if (id != doctor.doctor_id)
            {
                return NotFound();
            }

            // Business Logic: Validate email uniqueness (excluding current doctor)
            var existingDoctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.email == doctor.email && d.doctor_id != id);

            if (existingDoctor != null)
            {
                ModelState.AddModelError("email", "Email already exists in the system.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor information updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.doctor_id))
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
            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name", doctor.department_id);
            return View(doctor);
        }

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.doctor_id == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==================== DOCTOR DASHBOARD ====================

        // GET: Doctors/Dashboard/5
        public async Task<IActionResult> Dashboard(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.doctor_id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Calculate statistics
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var todayAppointments = doctor.Appointments?
                .Where(a => a.appointment_date.Date == today)  // REMOVED .HasValue
                .Count() ?? 0;

            var monthAppointments = doctor.Appointments?
                .Where(a => a.appointment_date >= monthStart && a.appointment_date <= monthEnd)  // REMOVED .HasValue
                .Count() ?? 0;

            var completedAppointments = doctor.Appointments?
                .Where(a => a.status == "Completed")
                .Count() ?? 0;

            var totalPatients = doctor.Appointments?
                .Select(a => a.patient_id)
                .Distinct()
                .Count() ?? 0;

            var totalRevenue = doctor.Appointments?
                .Where(a => a.Billing != null)
                .Sum(a => a.Billing.total_amount) ?? 0;

            ViewBag.TodayAppointments = todayAppointments;
            ViewBag.MonthAppointments = monthAppointments;
            ViewBag.CompletedAppointments = completedAppointments;
            ViewBag.TotalPatients = totalPatients;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.Today = today.ToString("MMMM dd, yyyy");

            return View(doctor);
        }

        // ==================== DOCTOR APPOINTMENTS ====================

        // GET: Doctors/Appointments/5
        public async Task<IActionResult> Appointments(int? id, string status = "all", DateTime? date = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            var appointmentsQuery = _context.Appointments
                .Where(a => a.doctor_id == id)
                .Include(a => a.Patient)
                .Include(a => a.MedicalRecord)
                .Include(a => a.Billing)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.status == status);
            }

            // Filter by date
            if (date.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.appointment_date.Date == date.Value.Date);  // REMOVED .HasValue
            }

            var appointments = await appointmentsQuery
                .OrderByDescending(a => a.appointment_date)
                .ToListAsync();

            ViewBag.Doctor = doctor;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDate = date;
            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "all", Text = "All Appointments" },
                new { Value = "Scheduled", Text = "Scheduled" },
                new { Value = "Confirmed", Text = "Confirmed" },
                new { Value = "In Progress", Text = "In Progress" },
                new { Value = "Completed", Text = "Completed" },
                new { Value = "Cancelled", Text = "Cancelled" }
            }, "Value", "Text", status);

            return View(appointments);
        }

        // GET: Doctors/MedicalRecords/5
        public async Task<IActionResult> MedicalRecords(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            // Get medical records through appointments
            var medicalRecords = await _context.MedicalRecords
                .Where(mr => mr.Appointment.doctor_id == id)
                .Include(mr => mr.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(mr => mr.Prescriptions)
                .Include(mr => mr.TestResults)
                .OrderByDescending(mr => mr.visit_date)
                .ToListAsync();

            ViewBag.Doctor = doctor;
            return View(medicalRecords);
        }

        // GET: Doctors/Prescriptions/5
        public async Task<IActionResult> Prescriptions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            // Get prescriptions through medical records and appointments
            var prescriptions = await _context.Prescriptions
                .Where(p => p.MedicalRecord.Appointment.doctor_id == id)
                .Include(p => p.MedicalRecord)
                    .ThenInclude(mr => mr.Appointment)
                        .ThenInclude(a => a.Patient)
                .OrderByDescending(p => p.MedicalRecord.visit_date)
                .ToListAsync();

            ViewBag.Doctor = doctor;
            return View(prescriptions);
        }

        // ==================== DOCTOR SCHEDULE ====================

        // GET: Doctors/Schedule/5
        public async Task<IActionResult> Schedule(int? id, DateTime? weekStart)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            var startDate = weekStart ?? GetStartOfWeek(DateTime.Today);
            var endDate = startDate.AddDays(7);

            // Get schedules
            var schedules = await _context.DoctorSchedules
                .Where(ds => ds.doctor_id == id &&
                            ds.available_date >= startDate &&
                            ds.available_date < endDate)
                .OrderBy(ds => ds.available_date)
                .ThenBy(ds => ds.start_time)
                .ToListAsync();

            // Get appointments
            var appointments = await _context.Appointments
                .Where(a => a.doctor_id == id &&
                           a.appointment_date >= startDate &&
                           a.appointment_date < endDate)  // REMOVED .HasValue
                .Include(a => a.Patient)
                .OrderBy(a => a.appointment_date)
                .ToListAsync();

            ViewBag.Doctor = doctor;
            ViewBag.WeekStart = startDate;
            ViewBag.WeekEnd = endDate.AddDays(-1);
            ViewBag.Schedules = schedules;
            ViewBag.Appointments = appointments;

            return View();
        }

        // POST: Doctors/SetSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetSchedule(int doctorId, DateTime scheduleDate,
            DateTime startTime, DateTime endTime)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if schedule exists
            var existingSchedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(ds => ds.doctor_id == doctorId &&
                                          ds.available_date.Date == scheduleDate.Date);

            if (existingSchedule != null)
            {
                existingSchedule.start_time = startTime;
                existingSchedule.end_time = endTime;
                _context.Update(existingSchedule);
                TempData["SuccessMessage"] = $"Schedule updated for {scheduleDate:MM/dd/yyyy}";
            }
            else
            {
                var schedule = new DoctorSchedule
                {
                    doctor_id = doctorId,
                    available_date = scheduleDate.Date,
                    start_time = startTime,
                    end_time = endTime
                };
                _context.Add(schedule);
                TempData["SuccessMessage"] = $"New schedule created for {scheduleDate:MM/dd/yyyy}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Schedule), new { id = doctorId });
        }

        // ==================== DOCTOR PERFORMANCE ====================

        // GET: Doctors/Performance/5
        public async Task<IActionResult> Performance(int? id, DateTime? fromDate, DateTime? toDate)
        {
            if (id == null)
            {
                return NotFound();
            }

            var startDate = fromDate ?? DateTime.Today.AddMonths(-3);
            var endDate = toDate ?? DateTime.Today;

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.doctor_id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Get appointments
            var appointments = await _context.Appointments
                .Where(a => a.doctor_id == id &&
                           a.appointment_date >= startDate &&
                           a.appointment_date <= endDate)  // REMOVED .HasValue
                .Include(a => a.Billing)
                .ToListAsync();

            // Get medical records
            var medicalRecords = await _context.MedicalRecords
                .Where(mr => mr.Appointment.doctor_id == id &&
                           mr.visit_date >= startDate &&
                           mr.visit_date <= endDate)
                .Include(mr => mr.Prescriptions)
                .Include(mr => mr.TestResults)
                .ToListAsync();

            // Calculate statistics
            var totalAppointments = appointments.Count;
            var completedAppointments = appointments.Count(a => a.status == "Completed");
            var cancelledAppointments = appointments.Count(a => a.status == "Cancelled");
            var noShowAppointments = appointments.Count(a => a.status == "No-show");

            var completionRate = totalAppointments > 0 ?
                Math.Round((decimal)completedAppointments / totalAppointments * 100, 2) : 0;

            var cancellationRate = totalAppointments > 0 ?
                Math.Round((decimal)cancelledAppointments / totalAppointments * 100, 2) : 0;

            var totalRevenue = appointments
                .Where(a => a.Billing != null)
                .Sum(a => a.Billing.total_amount);

            var prescriptionsCount = medicalRecords
                .Sum(mr => mr.Prescriptions?.Count ?? 0);

            var testsCount = medicalRecords
                .Sum(mr => mr.TestResults?.Count ?? 0);

            // Monthly statistics
            var monthlyStats = appointments
                .GroupBy(a => new { a.appointment_date.Year, a.appointment_date.Month })  // REMOVED .Value
                .Select(g => new MonthlyStat
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Count(),
                    Completed = g.Count(a => a.status == "Completed"),
                    Revenue = g.Where(a => a.Billing != null).Sum(a => a.Billing.total_amount)
                })
                .OrderBy(s => s.Month)
                .ToList();

            ViewBag.Doctor = doctor;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TotalAppointments = totalAppointments;
            ViewBag.CompletedAppointments = completedAppointments;
            ViewBag.CancelledAppointments = cancelledAppointments;
            ViewBag.NoShowAppointments = noShowAppointments;
            ViewBag.CompletionRate = completionRate;
            ViewBag.CancellationRate = cancellationRate;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PrescriptionsCount = prescriptionsCount;
            ViewBag.TestsCount = testsCount;
            ViewBag.MonthlyStats = monthlyStats;

            return View();
        }

        // ==================== HELPER METHODS ====================

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = date.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0)
            {
                diff += 7;
            }
            return date.AddDays(-diff).Date;
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.doctor_id == id);
        }

        // Helper class for monthly statistics
        public class MonthlyStat
        {
            public string Month { get; set; }
            public int Total { get; set; }
            public int Completed { get; set; }
            public decimal Revenue { get; set; }
        }
    }

   
}