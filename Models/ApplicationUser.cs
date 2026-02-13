using Microsoft.AspNetCore.Identity;

namespace QuoteManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Full name for display (required)
        public string FullName { get; set; } = string.Empty;

        // Who created this user account?
        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        // When was account created?
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Additional info (optional)
        public string? Address { get; set; }

        // List of users this person created
        public ICollection<ApplicationUser> CreatedUsers { get; set; } = new List<ApplicationUser>();
    }
}