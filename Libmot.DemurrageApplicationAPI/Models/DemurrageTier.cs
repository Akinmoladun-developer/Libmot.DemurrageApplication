namespace Libmot.DemurrageApplicationAPI.Models
{
    public class DemurrageTier
    {
        public int Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public int DayFrom { get; set; }
        public int DayTo { get; set; }
        public decimal RatePerDay { get; set; }  // Demurrage charge per day
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
