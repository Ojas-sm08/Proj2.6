using System.Collections.Generic;

namespace HospitalManagementSystem.Models
{
    public class PatientDetailsViewModel
    {
        public Patient? Patient { get; set; }
        public List<PatientFile>? PatientFiles { get; set; } = new List<PatientFile>();
    }
}
