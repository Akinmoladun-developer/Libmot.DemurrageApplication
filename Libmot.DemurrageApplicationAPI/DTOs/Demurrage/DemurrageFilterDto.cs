namespace Libmot.DemurrageApplicationAPI.DTOs.Demurrage
{
    public class DemurrageFilterDto
    {
        public int? ShipmentId { get; set; }
        public int? CustomerId { get; set; }
        public bool? IsSettled { get; set; }
        public DateTime? AccrualDateFrom { get; set; }
        public DateTime? AccrualDateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
