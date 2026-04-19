namespace Libmot.DemurrageApplicationAPI.Helpers
{
    public static class NigerianStates
    {
        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "Abia", "Adamawa", "Akwa Ibom", "Anambra", "Bauchi", "Bayelsa",
        "Benue", "Borno", "Cross River", "Delta", "Ebonyi", "Edo",
        "Ekiti", "Enugu", "FCT", "Gombe", "Imo", "Jigawa",
        "Kaduna", "Kano", "Katsina", "Kebbi", "Kogi", "Kwara",
        "Lagos", "Nasarawa", "Niger", "Ogun", "Ondo", "Osun",
        "Oyo", "Plateau", "Rivers", "Sokoto", "Taraba", "Yobe", "Zamfara"
    };

        public static bool IsValid(string state) => All.Contains(state);
    }
}
