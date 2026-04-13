using Libmot.DemurrageApplicationAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class UpdateShipmentDto
    {
       
        public DateTime? ActualReleaseDate { get; set; }

        public ShipmentStatus Status { get; set; }

        public string? AssignedDriverId { get; set; }

        [Range(1, 365)]
        public int FreeDaysAllowed { get; set; }
    }
}
