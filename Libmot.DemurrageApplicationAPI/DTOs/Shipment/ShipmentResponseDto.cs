namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public string WaybillNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? DriverUserId { get; set; }
        public string? DriverName { get; set; }
        public string OriginState { get; set; } = string.Empty;
        public string DestinationState { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public decimal DeclaredValue { get; set; }
        public decimal Weight { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpectedArrivalDate { get; set; }
        public DateTime? ActualArrivalDate { get; set; }
        public DateTime? FreeDaysExpiry { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public bool IsDemurrageActive { get; set; }
        public int? DaysInStorage { get; set; }
        public int? DemurrageDays { get; set; }        // days beyond free period
        public decimal? AccruedDemurrageAmount { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
    }

    public class ShipmentListResponseDto
    {
        public int Id { get; set; }
        public string WaybillNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public string OriginState { get; set; } = string.Empty;
        public string DestinationState { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ActualArrivalDate { get; set; }
        public DateTime? FreeDaysExpiry { get; set; }
        public bool IsDemurrageActive { get; set; }
        public decimal? AccruedDemurrageAmount { get; set; }
    }
}
