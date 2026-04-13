using Libmot.DemurrageApplicationAPI.Models;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class ShipmentFilterDto
    {
        //Implementing Pagination
        public string? SearchTerm { get; set; }         
        public ShipmentStatus? Status { get; set; }
        public string? CustomerId { get; set; }
        public DateTime? ArrivalDateFrom { get; set; }
        public DateTime? ArrivalDateTo { get; set; }
        public bool? IsDemurrageActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
