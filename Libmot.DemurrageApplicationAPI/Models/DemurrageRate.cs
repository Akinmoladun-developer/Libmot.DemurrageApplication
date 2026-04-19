namespace Libmot.DemurrageApplication.Models;

public class DemurrageRate
{
    public int Id { get; set; }
    public string TierName { get; set; } = string.Empty;   
    public int DayFrom { get; set; }                       
    public int DayTo { get; set; }                          
    public decimal DailyRate { get; set; }                  
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<DemurrageAccrual> Accruals { get; set; } = new List<DemurrageAccrual>();
}