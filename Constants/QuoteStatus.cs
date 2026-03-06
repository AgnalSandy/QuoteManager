namespace QuoteManager.Constants
{
    /// <summary>
    /// Quote status constants
    /// </summary>
    public static class QuoteStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";

        public static readonly string[] All = new[]
        {
            Pending, Accepted, Rejected, Expired
        };

        public static bool IsValid(string status)
        {
            return Array.Exists(All, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }
    }
}
