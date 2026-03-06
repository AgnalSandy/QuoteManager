namespace QuoteManager.Constants
{
    /// <summary>
    /// Invoice status constants
    /// </summary>
    public static class InvoiceStatus
    {
        public const string Unpaid = "Unpaid";
        public const string Paid = "Paid";
        public const string PartiallyPaid = "Partially Paid";
        public const string Overdue = "Overdue";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = new[]
        {
            Unpaid, Paid, PartiallyPaid, Overdue, Cancelled
        };

        public static bool IsValid(string status)
        {
            return Array.Exists(All, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }
    }
}
