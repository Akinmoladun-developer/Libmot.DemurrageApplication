using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class RecordPickupDto
    {
        public DateTime? PickupDate { get; set; }

        [MaxLength(200)]
        public string? ReceivedBy { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
