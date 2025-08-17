using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ProfileModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? Email { get; set; }
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last Name")]
            [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Current Password")]
            [DataType(DataType.Password)]
            public string? CurrentPassword { get; set; }

            [Display(Name = "New Password")]
            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            public string? NewPassword { get; set; }

            [Display(Name = "Confirm New Password")]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmNewPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when accessing profile page");
                return NotFound("User not found.");
            }

            Email = user.Email;
            Input = new InputModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when updating profile");
                return NotFound("User not found.");
            }

            Email = user.Email;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update name fields
            bool hasChanges = false;

            if (user.FirstName != Input.FirstName)
            {
                user.FirstName = Input.FirstName;
                hasChanges = true;
            }

            if (user.LastName != Input.LastName)
            {
                user.LastName = Input.LastName;
                hasChanges = true;
            }

            // Handle password change if provided
            bool passwordChanged = false;
            if (!string.IsNullOrEmpty(Input.CurrentPassword) && !string.IsNullOrEmpty(Input.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
                if (changePasswordResult.Succeeded)
                {
                    passwordChanged = true;
                    _logger.LogInformation("User {UserId} changed their password successfully", user.Id);
                }
                else
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }
            else if (!string.IsNullOrEmpty(Input.CurrentPassword) || !string.IsNullOrEmpty(Input.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "Both current password and new password are required to change password.");
                return Page();
            }

            // Save name changes
            if (hasChanges)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
                _logger.LogInformation("User {UserId} updated their profile information", user.Id);
            }

            // Set status message
            if (hasChanges && passwordChanged)
            {
                StatusMessage = "Your profile and password have been updated successfully.";
            }
            else if (hasChanges)
            {
                StatusMessage = "Your profile has been updated successfully.";
            }
            else if (passwordChanged)
            {
                StatusMessage = "Your password has been changed successfully.";
            }
            else
            {
                StatusMessage = "No changes were made to your profile.";
            }

            // If password was changed, refresh the sign-in to prevent logout
            if (passwordChanged)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            return RedirectToPage();
        }
    }
}
