namespace Libmot.DemurrageApplicationAPI.DTOs.Invoice
{
    public class InvoiceFilterDto
    {
        public string? InvoiceNumber { get; set; }
        public int? CustomerId { get; set; }
        public int? ShipmentId { get; set; }
        public string? Status { get; set; }
        public bool? IsOverdue { get; set; }
        public DateTime? IssuedFrom { get; set; }
        public DateTime? IssuedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
