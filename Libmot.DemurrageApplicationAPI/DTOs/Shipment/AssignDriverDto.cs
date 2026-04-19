using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class AssignDriverDto
    {
        [Required]
        public string DriverUserId { get; set; } = string.Empty;
    }
}
