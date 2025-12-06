namespace Hospital.Models
{
    public class DoctorSchedule
    {
        public int schedule_id { get; set; }
        public int doctor_id { get; set; }
        public DateTime available_date { get; set; }
        public DateTime start_time { get; set; }
        public DateTime end_time { get; set; }

        public Doctor Doctor { get; set; }
    }
}