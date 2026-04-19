using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.RateConfig
{
    public class CreateDemurrageRateDto
    {
        [Required]
        [MaxLength(100)]
        public string TierName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int DayFrom { get; set; }

        /// <summary>0 means unlimited (open-ended final tier)</summary>
        [Range(0, int.MaxValue)]
        public int DayTo { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal DailyRate { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class UpdateDemurrageRateDto
    {
        [MaxLength(100)]
        public string? TierName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DailyRate { get; set; }

        public bool? IsActive { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class DemurrageRateResponseDto
    {
        public int Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public int DayFrom { get; set; }
        public int DayTo { get; set; }
        public string DayRangeLabel { get; set; } = string.Empty; // "Day 8–14"
        public decimal DailyRate { get; set; }
        public bool IsActive { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
