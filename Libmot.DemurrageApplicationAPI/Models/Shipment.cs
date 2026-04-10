namespace Libmot.DemurrageApplicationAPI.Models
{
    public enum ShipmentStatus
    {
        Pending, InTransit, Released, Delivered, Disputed
    }

    public class Shipment
    {
        public int Id { get; set; }

        // public string ShipmentCode { get; set; } = string.Empty; 
        public string TrackingNumber { get; set; } = string.Empty; // OK342764WR. Sample tracking mumber OKOTA TO WARRI
        public DateTime ArrivalDate { get; set; }
        public DateTime? FreeUntilDate { get; set; }       // Last free day in our warehouse
        public DateTime? ActualReleaseDate { get; set; }   // When item was picked up from warehouse/terminals
        public ShipmentStatus Status { get; set; }
        public int FreeDaysAllowed { get; set; } = 7;      // Default free days allowed 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // FK to access customer's
        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = null!;

        // FK - Assigned Driver
        public string? AssignedDriverId { get; set; }
        public ApplicationUser? AssignedDriver { get; set; }

        public ICollection<DemurrageRecord> DemurrageRecords { get; set; } = new List<DemurrageRecord>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
