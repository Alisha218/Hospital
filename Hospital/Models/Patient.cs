using System;
using System.Collections.Generic;

namespace Hospital.Models
{
    public class Patient
    {
        public int patient_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string gender { get; set; }
        public DateTime date_of_birth { get; set; }
        public string contact_number { get; set; }
        public string email { get; set; }
        public string address { get; set; }
        public string blood_group { get; set; }

        public ICollection<Appointment> Appointments { get; set; }

    }
}