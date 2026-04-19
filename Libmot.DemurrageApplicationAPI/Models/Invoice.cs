using LibmotExpress.DemurrageApi.Models;

namespace Libmot.DemurrageApplication.Models;
public enum InvoiceStatus
{
    Unpaid,
    PartiallyPaid,
    Paid,
    Overdue,
    Disputed,
    Cancelled
}
public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? PdfPath { get; set; }
    public string? Notes { get; set; }
    public bool IsSystemGenerated { get; set; } = true;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

