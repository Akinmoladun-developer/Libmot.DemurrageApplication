namespace Libmot.DemurrageApplicationAPI.DTOs.Demurrage
{
    public class DemurrageAccrualResponseDto
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public string WaybillNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public DateTime AccrualDate { get; set; }
        public int DayCount { get; set; }
        public string TierApplied { get; set; } = string.Empty;
        public decimal DailyRateApplied { get; set; }
        public decimal AccumulatedTotal { get; set; }
        public bool IsSettled { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DemurrageSummaryDto
    {
        public int ShipmentId { get; set; }
        public string WaybillNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public string OriginState { get; set; } = string.Empty;
        public string DestinationState { get; set; } = string.Empty;
        public DateTime ArrivalDate { get; set; }
        public DateTime FreeDaysExpiry { get; set; }
        public int TotalDaysInStorage { get; set; }
        public int FreeDays { get; set; }
        public int DemurrageDays { get; set; }
        public decimal TotalAccruedAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string ShipmentStatus { get; set; } = string.Empty;
        public List<DemurrageAccrualResponseDto> DailyBreakdown { get; set; } = new();
    }
}
