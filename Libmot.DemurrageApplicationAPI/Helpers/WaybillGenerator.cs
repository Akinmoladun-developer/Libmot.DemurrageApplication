namespace Libmot.DemurrageApplicationAPI.Helpers
{
    public static class WaybillGenerator
    {
        /// <summary>
        /// Generates a unique waybill number.
        /// Format: LMX-{YEAR}{MONTH}-{6 random alphanumeric chars}
        /// Example: LMX-202501-A3K9PZ
        /// </summary>
        public static string Generate()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMM");
            var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"LMX-{datePart}-{randomPart}";
        }
    }
}
