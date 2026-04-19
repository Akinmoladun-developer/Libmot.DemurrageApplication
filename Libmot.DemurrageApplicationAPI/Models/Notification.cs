using LibmotExpress.DemurrageApi.Models;

namespace Libmot.DemurrageApplication.Models;
public class Notification
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Message { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    ItemArrived,
    FreeDaysExpiringSoon,   // Warning merchant of demurrage fee on day 5 
    DemurrageStarted,       // Warning merchant of demurrage fee on day 8
    DemurrageReminder,      // This reminder is daily and also periodic depending on merchant's response to reminder by SMS or Email
    InvoiceGenerated,
    PaymentReceived,
    DisputeReceived,
    DisputeResolved
}

public enum NotificationChannel
{
    Email,
    SMS,
    Both
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}