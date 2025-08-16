using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        
        public List<Company> Companies { get; set; } = new();
        public List<UserGroup> UserGroups { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Company")]
            public int? CompanyId { get; set; }

            [Display(Name = "User Groups")]
            public List<int> SelectedGroupIds { get; set; } = new();

            [Display(Name = "Email Confirmed")]
            public bool EmailConfirmed { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadData();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                CompanyId = Input.CompanyId,
                EmailConfirmed = Input.EmailConfirmed,
                DateCreated = DateTime.UtcNow,
                IsFirstLogin = true,
                IsProfileComplete = false
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Add user to selected groups
                if (Input.SelectedGroupIds.Any())
                {
                    var memberships = Input.SelectedGroupIds.Select(groupId => new UserGroupMembership
                    {
                        UserId = user.Id,
                        UserGroupId = groupId,
                        DateJoined = DateTime.UtcNow,
                        JoinedBy = User.Identity!.Name,
                        IsActive = true
                    }).ToList();

                    _context.UserGroupMemberships.AddRange(memberships);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"User '{Input.FirstName} {Input.LastName}' has been created successfully.";
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private async Task LoadData()
        {
            Companies = await _context.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

            UserGroups = await _context.UserGroups
                .Include(g => g.Company)
                .OrderBy(g => g.Company.Name)
                .ThenBy(g => g.Name)
                .ToListAsync();
        }
    }
}
