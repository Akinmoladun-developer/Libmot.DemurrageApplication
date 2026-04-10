namespace Libmot.DemurrageApplicationAPI.Models
{
    public enum InvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled }
    public enum PaymentMethod { BankTransfer, Cash, OnlinePayment }

    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;  // e.g. INV-2024-00001
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }        // VAT 7.5%
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; } = 0;
        public decimal BalanceDue => TotalAmount - AmountPaid;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public PaymentMethod? PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;

        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = null!;

        public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
        public ICollection<DemurrageRecord> DemurrageRecords { get; set; } = new List<DemurrageRecord>();
    }
}
