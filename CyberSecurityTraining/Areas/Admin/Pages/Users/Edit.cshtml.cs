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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        
        public List<Company> Companies { get; set; } = new();
        public List<UserGroup> UserGroups { get; set; } = new();
        public new ApplicationUser User { get; set; } = null!;

        public class InputModel
        {
            public string Id { get; set; } = string.Empty;

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

            [Display(Name = "Company")]
            public int? CompanyId { get; set; }

            [Display(Name = "User Groups")]
            public List<int> SelectedGroupIds { get; set; } = new();

            [Display(Name = "Email Confirmed")]
            public bool EmailConfirmed { get; set; }

            [Display(Name = "Account Locked")]
            public bool IsLocked { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await LoadUser(user);
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadData();

            if (!ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(Input.Id);
                if (user != null)
                {
                    await LoadUser(user);
                }
                return Page();
            }

            var userToUpdate = await _userManager.FindByIdAsync(Input.Id);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Update basic properties
            userToUpdate.FirstName = Input.FirstName;
            userToUpdate.LastName = Input.LastName;
            userToUpdate.Email = Input.Email;
            userToUpdate.UserName = Input.Email;
            userToUpdate.CompanyId = Input.CompanyId;
            userToUpdate.EmailConfirmed = Input.EmailConfirmed;

            var result = await _userManager.UpdateAsync(userToUpdate);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadUser(userToUpdate);
                return Page();
            }

            // Handle account lock/unlock
            if (Input.IsLocked)
            {
                await _userManager.SetLockoutEndDateAsync(userToUpdate, DateTimeOffset.MaxValue);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(userToUpdate, null);
            }

            // Update group memberships
            var currentMemberships = await _context.UserGroupMemberships
                .Where(m => m.UserId == Input.Id)
                .ToListAsync();

            // Remove old memberships that are not selected
            var membershipsToRemove = currentMemberships
                .Where(m => !Input.SelectedGroupIds.Contains(m.UserGroupId))
                .ToList();

            _context.UserGroupMemberships.RemoveRange(membershipsToRemove);

            // Add new memberships
            var currentGroupIds = currentMemberships.Select(m => m.UserGroupId).ToList();
            var newGroupIds = Input.SelectedGroupIds.Except(currentGroupIds).ToList();

            var newMemberships = newGroupIds.Select(groupId => new UserGroupMembership
            {
                UserId = Input.Id,
                UserGroupId = groupId,
                DateJoined = DateTime.UtcNow,
                JoinedBy = HttpContext.User.Identity?.Name ?? "Admin",
                IsActive = true
            }).ToList();

            _context.UserGroupMemberships.AddRange(newMemberships);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{Input.FirstName} {Input.LastName}' has been updated successfully.";
            return RedirectToPage("./Index");
        }

        private async Task LoadUser(ApplicationUser user)
        {
            User = user;
            
            var memberships = await _context.UserGroupMemberships
                .Where(m => m.UserId == user.Id && m.IsActive)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            Input = new InputModel
            {
                Id = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CompanyId = user.CompanyId,
                SelectedGroupIds = memberships,
                EmailConfirmed = user.EmailConfirmed,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            };
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
