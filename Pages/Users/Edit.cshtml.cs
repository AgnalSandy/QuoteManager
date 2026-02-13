using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuoteManager.Constants;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Users
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public EditUserViewModel Input { get; set; } = new EditUserViewModel();

        public List<string> AvailableRoles { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "No Role";

            Input = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                CurrentRole = currentRole
            };

            AvailableRoles = GetAvailableRolesForCurrentUser();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                AvailableRoles = GetAvailableRolesForCurrentUser();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = Input.FullName;
            user.Email = Input.Email;
            user.UserName = Input.Email;
            user.PhoneNumber = Input.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Update role if changed
                if (!string.IsNullOrEmpty(Input.NewRole) && Input.NewRole != Input.CurrentRole)
                {
                    if (CanCreateRole(Input.NewRole))
                    {
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, Input.NewRole);
                    }
                }

                TempData["Success"] = "User updated successfully!";
                return RedirectToPage("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            AvailableRoles = GetAvailableRolesForCurrentUser();
            return Page();
        }

        private List<string> GetAvailableRolesForCurrentUser()
        {
            if (User.IsInRole(Roles.SuperAdmin))
            {
                return new List<string> { Roles.Admin, Roles.Staff, Roles.Client };
            }
            else if (User.IsInRole(Roles.Admin))
            {
                return new List<string> { Roles.Staff, Roles.Client };
            }

            return new List<string>();
        }

        private bool CanCreateRole(string role)
        {
            if (User.IsInRole(Roles.SuperAdmin))
            {
                return true;
            }
            else if (User.IsInRole(Roles.Admin))
            {
                return role == Roles.Staff || role == Roles.Client;
            }

            return false;
        }
    }
}
