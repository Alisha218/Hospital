using Microsoft.EntityFrameworkCore;
using Hospital.Models;

namespace Hospital.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Billing> Billings { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define primary keys
            modelBuilder.Entity<Department>().HasKey(d => d.department_id);
            modelBuilder.Entity<Doctor>().HasKey(d => d.doctor_id);
            modelBuilder.Entity<DoctorSchedule>().HasKey(ds => ds.schedule_id);
            modelBuilder.Entity<Patient>().HasKey(p => p.patient_id);
            modelBuilder.Entity<Appointment>().HasKey(a => a.appointment_id);
            modelBuilder.Entity<MedicalRecord>().HasKey(mr => mr.record_id);
            modelBuilder.Entity<Prescription>().HasKey(pr => pr.prescription_id);
            modelBuilder.Entity<Billing>().HasKey(b => b.bill_id);
            modelBuilder.Entity<TestResult>().HasKey(tr => tr.test_id);


            // Define relationships - SABHI ME .OnDelete(DeleteBehavior.ClientSetNull) ADD KAR DIYA
            // Department -> Doctors (1:N)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dept => dept.Doctors)
                .HasForeignKey(d => d.department_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // Doctor -> DoctorSchedules (1:N)
            modelBuilder.Entity<DoctorSchedule>()
                .HasOne(ds => ds.Doctor)
                .WithMany(d => d.DoctorSchedules)
                .HasForeignKey(ds => ds.doctor_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // Patient -> Appointments (1:N)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.patient_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // Doctor -> Appointments (1:N)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.doctor_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // Appointment -> MedicalRecord (1:1) 
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Appointment)
                .WithOne(a => a.MedicalRecord)
                .HasForeignKey<MedicalRecord>(mr => mr.appointment_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // MedicalRecord -> Prescriptions (1:N)
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.MedicalRecord)
                .WithMany(mr => mr.Prescriptions)
                .HasForeignKey(p => p.record_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // MedicalRecord -> TestResults (1:N)
            modelBuilder.Entity<TestResult>()
                .HasOne(tr => tr.MedicalRecord)
                .WithMany(mr => mr.TestResults)
                .HasForeignKey(tr => tr.record_id)
                .OnDelete(DeleteBehavior.ClientSetNull); // Matches ON DELETE SET NULL

            // Appointment -> Billing (1:1)
            modelBuilder.Entity<Billing>()
                .HasOne(b => b.Appointment)
                .WithOne(a => a.Billing)
                .HasForeignKey<Billing>(b => b.appointment_id)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}