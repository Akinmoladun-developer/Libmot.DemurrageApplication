using Libmot.DemurrageApplicationAPI.Models;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class ContainerResponseDto
    {
        public int Id { get; set; }
        public string ContainerNumber { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal WeightKg { get; set; }
        public string? Description { get; set; }
        public ContainerStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public DateTime CreatedAt { get; set; }
    }
}
