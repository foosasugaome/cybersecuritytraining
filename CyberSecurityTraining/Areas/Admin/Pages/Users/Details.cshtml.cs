using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser User { get; set; } = null!;
        public List<UserGroupMembership> GroupMemberships { get; set; } = new();
        public List<string> UserRoles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            User = user;

            // Load group memberships
            GroupMemberships = await _context.UserGroupMemberships
                .Include(m => m.UserGroup)
                    .ThenInclude(g => g.Company)
                .Where(m => m.UserId == id)
                .OrderBy(m => m.UserGroup.Company.Name)
                .ThenBy(m => m.UserGroup.Name)
                .ToListAsync();

            // Load user roles
            UserRoles = (await _userManager.GetRolesAsync(user)).ToList();

            return Page();
        }
    }
}
