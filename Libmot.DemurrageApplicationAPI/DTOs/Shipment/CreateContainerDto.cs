using Libmot.DemurrageApplicationAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class CreateContainerDto
    {
        [Required]
        public string ContainerNumber { get; set; } = string.Empty;

        [Required]
        public ContainerSize Size { get; set; }

        [Range(1, 999999)]
        public decimal WeightKg { get; set; }

        public string? Description { get; set; }
    }
}
