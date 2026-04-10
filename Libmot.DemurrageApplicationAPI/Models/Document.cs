namespace Libmot.DemurrageApplicationAPI.Models
{
    public enum DocumentType { Invoice, DemurrageReport, ExcelReport, Other }

    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DocumentType Type { get; set; }
        public long FileSizeBytes { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedById { get; set; } = string.Empty;

        public int? ShipmentId { get; set; }
        public Shipment? Shipment { get; set; }
    }
}
