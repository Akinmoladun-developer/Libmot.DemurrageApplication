using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class UpdateShipmentDto
    {
        [MaxLength(500)]
        public string? ItemDescription { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DeclaredValue { get; set; }

        [Range(0.001, double.MaxValue)]
        public decimal? Weight { get; set; }

        public DateTime? ExpectedArrivalDate { get; set; }

        public string? DriverUserId { get; set; }
    }
}
