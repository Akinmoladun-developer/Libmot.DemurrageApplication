using Libmot.DemurrageApplicationAPI.Models;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public string ShipmentCode { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime ArrivalDate { get; set; }
        public DateTime? FreeUntilDate { get; set; }
        public DateTime? ActualReleaseDate { get; set; }
        public int FreeDaysAllowed { get; set; }
        public ShipmentStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public int DaysOverFree { get; set; }
        public bool IsDemurrageActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Customer info
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        // Driver info
        public string? AssignedDriverId { get; set; }
        public string? AssignedDriverName { get; set; }

        
    }
}
