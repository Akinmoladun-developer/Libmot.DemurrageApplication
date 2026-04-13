namespace Libmot.DemurrageApplicationAPI.Models
{
    public enum ContainerSize { TwentyFT, FortyFT, FortyFiveHCFT }
    public enum ContainerStatus { AtPort, InTransit, Released, Delivered }

    public class Container
    {
        public int Id { get; set; }
        public string ContainerNumber { get; set; } = string.Empty;
        public ContainerSize Size { get; set; }
        public ContainerStatus Status { get; set; } = ContainerStatus.AtPort;
        public decimal WeightKg { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;

        public ICollection<DemurrageRecord> DemurrageRecords { get; set; } = new List<DemurrageRecord>();
    }
}
