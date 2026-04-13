using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class CreateShipmentDto
    {
        //[Required]
        public string BillOfLadingNumber { get; set; } = string.Empty;

        [Required]
        public string TrackingNumber { get; set; } = string.Empty;

        [Required]
        public DateTime ArrivalDate { get; set; }

        [Range(1, 365)]
        public int FreeDaysAllowed { get; set; } = 7;

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        public string? AssignedDriverId { get; set; }

        //[Required, MinLength(1)]
        //public List<CreateContainerDto> Containers { get; set; } = new();

    }
}
