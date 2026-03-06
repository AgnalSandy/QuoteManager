namespace QuoteManager.Constants
{
    /// <summary>
    /// Application role constants to avoid magic strings
    /// </summary>
    public static class ApplicationRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Client = "Client";

        public static class Policies
        {
            public const string RequireAdminRole = "RequireAdminRole";
            public const string RequireStaffRole = "RequireStaffRole";
            public const string RequireClientRole = "RequireClientRole";
        }

        public static string GetDisplayName(string role) => role switch
        {
            SuperAdmin => "Super Admin",
            Admin => "Administrator",
            Staff => "Staff Member",
            Client => "Client",
            _ => role
        };
    }
}
