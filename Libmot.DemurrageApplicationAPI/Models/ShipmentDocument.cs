namespace LibmotExpress.DemurrageApi.Models;

public class ShipmentDocument
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public enum DocumentType
{
    Waybill,
    Invoice,
    ProofOfDelivery,
    PaymentReceipt,
    Other
}