using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class CreateShipmentDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string OriginState { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DestinationState { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ItemDescription { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Declared value must be greater than zero.")]
        public decimal DeclaredValue { get; set; }

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Weight must be greater than zero.")]
        public decimal Weight { get; set; }

        public DateTime? ExpectedArrivalDate { get; set; }

        public string? DriverUserId { get; set; }

    }
}
