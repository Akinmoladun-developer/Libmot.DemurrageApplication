using Microsoft.AspNetCore.Identity;

namespace Libmot.DemurrageApplicationAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
