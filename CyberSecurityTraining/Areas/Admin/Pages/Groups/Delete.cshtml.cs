using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UserGroup UserGroup { get; set; } = default!;
        
        public int ActiveMembersCount { get; set; }
        public int TotalMembersCount { get; set; }
        public bool HasActiveMembers { get; set; }

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
            TotalMembersCount = userGroup.UserMemberships.Count;
            ActiveMembersCount = userGroup.UserMemberships.Count(m => m.IsActive);
            HasActiveMembers = ActiveMembersCount > 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.UserGroups == null)
            {
                return NotFound();
            }

            var userGroup = await _context.UserGroups
                .Include(g => g.UserMemberships)
                .Include(g => g.ModuleAssignments)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (userGroup != null)
            {
                // Check if there are active members
                var activeMembersCount = userGroup.UserMemberships.Count(m => m.IsActive);
                if (activeMembersCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete group with {activeMembersCount} active members. Please remove all members first.";
                    return RedirectToPage("./Details", new { id = id });
                }

                // Remove all memberships (including inactive ones)
                _context.UserGroupMemberships.RemoveRange(userGroup.UserMemberships);

                // Remove all module assignments
                if (userGroup.ModuleAssignments.Any())
                {
                    _context.GroupModuleAssignments.RemoveRange(userGroup.ModuleAssignments);
                }

                // Remove the group
                _context.UserGroups.Remove(userGroup);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Group '{userGroup.Name}' has been successfully deleted.";
            }

            return RedirectToPage("./Index");
        }
    }
}
