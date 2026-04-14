using LibmotExpress.DemurrageApi.Models;

namespace Libmot.DemurrageApplication.Models;
public class DemurrageAccrual
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public int DemurrageRateId { get; set; }
    public DemurrageRate DemurrageRate { get; set; } = null!;
    public DateTime AccrualDate { get; set; }               // The specific final date calculation for demu date
    public int DayCount { get; set; }                       
    public decimal DailyRateApplied { get; set; }
    public decimal AccumulatedTotal { get; set; }           // Calculates the total demurrage amount
    public bool IsSettled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}