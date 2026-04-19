using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Invoice
{
    // Manual invoice creation by Finance Officer.
    // System auto-creates on Day 8, but this allows manual override.
    
    public class CreateInvoiceDto
    {
        [Required]
        public int ShipmentId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal? OverrideAmount { get; set; }

        public DateTime? DueDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
