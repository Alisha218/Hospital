using System;

namespace Hospital.Models
{
    public class Prescription
    {
        public int prescription_id { get; set; }
        public int record_id { get; set; }
        public string medication_name { get; set; }
        public string dosage { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }

        public MedicalRecord MedicalRecord { get; set; }
    }
}