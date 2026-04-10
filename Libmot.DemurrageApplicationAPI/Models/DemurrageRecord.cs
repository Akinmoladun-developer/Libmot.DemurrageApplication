namespace Libmot.DemurrageApplicationAPI.Models
{
    public enum DemurrageStatus { Pending, Invoiced, Paid, Waived, Disputed }

    public class DemurrageRecord
    {
        public int Id { get; set; }
        public int DaysOverFree { get; set; }         // Total number of days an item overstayed our warehouse after the grace period
        public decimal TotalAmount { get; set; }       // Total amount of demurrage fee in our warehouse 
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public DemurrageStatus Status { get; set; } = DemurrageStatus.Pending;
        public string? Notes { get; set; }
        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;


        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
    }
}
