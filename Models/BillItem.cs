using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class BillItem
    {
        [Key]
        public int BillItemId { get; set; }

        [Required]
        public int BillId { get; set; }
        [ForeignKey("BillId")]
        public Bill? Bill { get; set; } // <<<< CHANGED: Made nullable with '?'

        [Required]
        [StringLength(255)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
    }
}
