using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserGroup UserGroup { get; set; } = default!;
        public List<UserGroupMembership> ActiveMembers { get; set; } = new();
        public int TotalMembersCount { get; set; }
        public int ActiveMembersCount { get; set; }
        public int AssignedModulesCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.UserGroups == null)
            {
                return NotFound();
            }

            var userGroup = await _context.UserGroups
                .Include(g => g.Company)
                .Include(g => g.UserMemberships)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userGroup == null)
            {
                return NotFound();
            }

            UserGroup = userGroup;
            
            // Get active members with user details
            ActiveMembers = userGroup.UserMemberships
                .Where(m => m.IsActive)
                .OrderBy(m => m.User.FirstName)
                .ThenBy(m => m.User.LastName)
                .ToList();

            TotalMembersCount = userGroup.UserMemberships.Count;
            ActiveMembersCount = userGroup.UserMemberships.Count(m => m.IsActive);

            // Get assigned modules count
            AssignedModulesCount = await _context.GroupModuleAssignments
                .Where(a => a.UserGroupId == id)
                .CountAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync(int groupId, string userId)
        {
            var membership = await _context.UserGroupMemberships
                .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId && m.IsActive);

            if (membership != null)
            {
                membership.IsActive = false;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "User has been removed from the group.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unable to find active membership to remove.";
            }

            return RedirectToPage(new { id = groupId });
        }
    }
}
