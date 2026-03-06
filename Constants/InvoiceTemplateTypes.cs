namespace QuoteManager.Constants
{
    /// <summary>
    /// Invoice template type constants
    /// </summary>
    public static class InvoiceTemplateTypes
    {
        public const string Professional = "Professional";
        public const string Minimal = "Minimal";
        public const string Modern = "Modern";

        /// <summary>
        /// Gets all available template types
        /// </summary>
        public static List<string> GetAll()
        {
            return new List<string>
            {
                Professional,
                Minimal,
                Modern
            };
        }

        /// <summary>
        /// Validates if a template type is valid
        /// </summary>
        public static bool IsValid(string templateType)
        {
            return GetAll().Contains(templateType);
        }
    }
}
