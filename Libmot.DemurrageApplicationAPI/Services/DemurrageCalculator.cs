using Libmot.DemurrageApplication.Models;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public static class DemurrageCalculator
    {
        private const int FREE_DAYS = 7; // 7 Days after deliveriy at different terminals before demurrage rate starts calculating

        /// <summary>
        /// Returns the number of days since arrival as of a given reference date.
        /// Day 1 = arrival date itself.
        /// </summary>
        public static int GetDayCount(DateTime arrivalDate, DateTime referenceDate)
            => (referenceDate.Date - arrivalDate.Date).Days + 1;

        /// <summary>
        /// Returns how many demurrage days have elapsed (0 during free period).
        /// </summary>
        public static int GetDemurrageDayNumber(DateTime arrivalDate, DateTime referenceDate)
        {
            var dayCount = GetDayCount(arrivalDate, referenceDate);
            return Math.Max(0, dayCount - FREE_DAYS);
        }

        /// <summary>
        /// Checks whether demurrage has started on the reference date.
        /// Demurrage starts on Day 8 (the day AFTER the 7th free day).
        /// </summary>
        public static bool IsDemurrageDay(DateTime arrivalDate, DateTime referenceDate)
            => GetDemurrageDayNumber(arrivalDate, referenceDate) > 0;

        /// <summary>
        /// Given the current day count and the active rate bands,
        /// returns the matching DemurrageRate tier.
        /// DayTo = 0 means the tier is open-ended (no upper limit).
        /// </summary>
        public static DemurrageRate? ResolveTier(int dayCount, IEnumerable<DemurrageRate> activeRates)
        {
            var demurrageDay = Math.Max(0, dayCount - FREE_DAYS);
            if (demurrageDay <= 0) return null;

            return activeRates
                .Where(r => r.IsActive
                         && r.DayFrom <= dayCount
                         && (r.DayTo == 0 || r.DayTo >= dayCount))
                .OrderByDescending(r => r.DayFrom)
                .FirstOrDefault();
        }

        /// <summary>
        /// Calculates the total accrued demurrage between two dates (inclusive)
        /// using the provided rate bands. Returns a day-by-day breakdown.
        /// </summary>
        public static List<(DateTime Date, int DayCount, DemurrageRate? Rate, decimal DailyCharge)>
            CalculateRange(DateTime arrivalDate, DateTime fromDate, DateTime toDate,
                           IEnumerable<DemurrageRate> activeRates)
        {
            var result = new List<(DateTime, int, DemurrageRate?, decimal)>();
            var ratesList = activeRates.ToList();
            var current = fromDate.Date;

            while (current <= toDate.Date)
            {
                var dayCount = GetDayCount(arrivalDate, current);
                var tier = ResolveTier(dayCount, ratesList);
                var dailyCharge = tier?.DailyRate ?? 0m;
                result.Add((current, dayCount, tier, dailyCharge));
                current = current.AddDays(1);
            }

            return result;
        }
    }
}
