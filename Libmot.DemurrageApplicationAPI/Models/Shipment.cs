using Libmot.DemurrageApplication.Models;

namespace LibmotExpress.DemurrageApi.Models;
public enum ShipmentStatus
{
    Pending,
    InTransit,
    ArrivedAtDestination,
    DemurrageActive,
    PickedUp,
    Closed,
    Cancelled
}



public class Shipment
{
    public int Id { get; set; }
    public string WaybillNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string? DriverUserId { get; set; }
    public ApplicationUser? Driver { get; set; }
    public string OriginState { get; set; } = string.Empty;
    public string DestinationState { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public decimal Weight { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ActualArrivalDate { get; set; }
    public DateTime? FreeDaysExpiry { get; set; }   // ArrivalDate + 7
    public DateTime? PickedUpAt { get; set; }
    public bool IsDemurrageActive { get; set; } = false;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ICollection<DemurrageAccrual> DemurrageAccruals { get; set; } = new List<DemurrageAccrual>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ShipmentDocument> Documents { get; set; } = new List<ShipmentDocument>();
    public ICollection<DisputeLog> DisputeLogs { get; set; } = new List<DisputeLog>();
}
