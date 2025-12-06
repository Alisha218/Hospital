using System.Collections.Generic;

namespace Hospital.Models
{
    public class Department
    {
        public int department_id { get; set; }
        public string name { get; set; }
        public string location { get; set; }

        public ICollection<Doctor> Doctors { get; set; }
    }
}