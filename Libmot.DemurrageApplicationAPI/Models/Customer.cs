using LibmotExpress.DemurrageApi.Models;

namespace Libmot.DemurrageApplication.Models;
public class Customer
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}