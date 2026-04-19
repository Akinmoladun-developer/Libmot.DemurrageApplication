namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class ShipmentFilterDto
    {
        public string? WaybillNumber { get; set; }
        public int? CustomerId { get; set; }
        public string? Status { get; set; }
        public string? OriginState { get; set; }
        public string? DestinationState { get; set; }
        public bool? IsDemurrageActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
