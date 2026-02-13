namespace QuoteManager.Constants
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Client = "Client";

        public static List<string> GetAllRoles()
        {
            return new List<string>
            {
                SuperAdmin,
                Admin,
                Staff,
                Client
            };
        }

        public static bool CanCreateRole(string creatorRole, string targetRole)
        {
            return (creatorRole, targetRole) switch
            {
                (SuperAdmin, Admin) => true,
                (SuperAdmin, Staff) => true,
                (SuperAdmin, Client) => true,
                (Admin, Staff) => true,
                (Admin, Client) => true,
                (Staff, Client) => true,
                _ => false
            };
        }
    }
}