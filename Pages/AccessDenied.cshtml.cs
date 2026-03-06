using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuoteManager.Pages
{
    public class AccessDeniedModel : PageModel
    {
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }
    }
}
