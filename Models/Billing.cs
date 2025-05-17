using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models
{
    public class Billing
    {
        public int Id { get; set; }

        [Required]
        public string PatientName { get; set; }

        [Required]
        public string Treatment { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime BillingDate { get; set; } = DateTime.Today;
    }
}
