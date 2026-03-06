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
                // Clients always go to their dashboard
                if (User.IsInRole("Client"))
                {
                    return RedirectToPage("/Client/Dashboard");
                }
                // Admin/Staff go to their dashboard
                else if (User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("SuperAdmin"))
                {
                    return RedirectToPage("/Dashboard/Index");
                }
                // If user has no recognized role, show access denied
                else
                {
                    return RedirectToPage("/AccessDenied");
                }
            }

            // Show public home page for unauthenticated users
            return Page();
        }
    }
}
