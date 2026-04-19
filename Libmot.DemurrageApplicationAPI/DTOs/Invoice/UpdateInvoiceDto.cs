using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Invoice
{
    public class UpdateInvoiceDto
    {
        public DateTime? DueDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

    }
}
