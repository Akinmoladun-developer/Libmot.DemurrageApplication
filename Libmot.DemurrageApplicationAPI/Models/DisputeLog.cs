using LibmotExpress.DemurrageApi.Models;

namespace Libmot.DemurrageApplication.Models;

public class DisputeLog
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string RaisedByUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? SupportingDocumentPath { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? ResolutionNotes { get; set; }
    public string? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum DisputeStatus
{
    Open,
    UnderReview,
    Resolved,
    Rejected
}