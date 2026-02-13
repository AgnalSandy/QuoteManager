using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuoteManager.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // If user is authenticated, handle role-based navigation
            if (User.Identity?.IsAuthenticated == true)
            {
                // Admin/Staff go to dashboard
                if (User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("SuperAdmin"))
                {
                    return RedirectToPage("/Dashboard/Index");
                }
                // Client stays on home page but scrolls to their info
                else if (User.IsInRole("Client"))
                {
                    // Client sees the home page with their info section
                    // The anchor #client-info will scroll them to their section
                    return Page();
                }
            }

            // Show public home page for unauthenticated users
            return Page();
        }
    }
}
