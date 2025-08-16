using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class ModuleAssignmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ModuleAssignmentsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserGroup UserGroup { get; set; } = default!;
        public List<ModuleAssignmentInfo> AssignedModules { get; set; } = new();
        public List<Module> AvailableModules { get; set; } = new();

        [BindProperty]
        public AssignModuleModel Input { get; set; } = new();

        public class AssignModuleModel
        {
            [Required]
            [Display(Name = "Module")]
            public int ModuleId { get; set; }

            [Display(Name = "Due Date (Optional)")]
            [DataType(DataType.Date)]
            public DateTime? DueDate { get; set; }
        }

        public class ModuleAssignmentInfo
        {
            public GroupModuleAssignment Assignment { get; set; } = null!;
            public int MemberCount { get; set; }
            public int CompletedCount { get; set; }
            public double CompletionPercentage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userGroup = await _context.UserGroups
                .Include(g => g.Company)
                .Include(g => g.UserMemberships.Where(m => m.IsActive))
                .FirstOrDefaultAsync(g => g.Id == id);

            if (userGroup == null)
            {
                return NotFound();
            }

            UserGroup = userGroup;
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAssignModuleAsync(int id)
        {
            var userGroup = await _context.UserGroups
                .Include(g => g.Company)
                .Include(g => g.UserMemberships.Where(m => m.IsActive))
                .FirstOrDefaultAsync(g => g.Id == id);

            if (userGroup == null)
            {
                return NotFound();
            }

            UserGroup = userGroup;

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            // Check if module is already assigned to this group
            var existingAssignment = await _context.GroupModuleAssignments
                .FirstOrDefaultAsync(a => a.UserGroupId == id && a.ModuleId == Input.ModuleId);

            if (existingAssignment != null)
            {
                ModelState.AddModelError("Input.ModuleId", "This module is already assigned to the group.");
                await LoadDataAsync();
                return Page();
            }

            // Validate module exists and is active
            var module = await _context.Modules.FindAsync(Input.ModuleId);
            if (module == null || !module.IsActive)
            {
                ModelState.AddModelError("Input.ModuleId", "Selected module is not available.");
                await LoadDataAsync();
                return Page();
            }

            // Create the assignment
            var assignment = new GroupModuleAssignment
            {
                UserGroupId = id,
                ModuleId = Input.ModuleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = HttpContext.User.Identity?.Name ?? "Admin",
                DueDate = Input.DueDate
            };

            _context.GroupModuleAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Module '{module.Title}' has been assigned to the group.";
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostRemoveAssignmentAsync(int groupId, int assignmentId)
        {
            var assignment = await _context.GroupModuleAssignments
                .Include(a => a.Module)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.UserGroupId == groupId);

            if (assignment != null)
            {
                _context.GroupModuleAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Module '{assignment.Module.Title}' has been removed from the group.";
            }
            else
            {
                TempData["ErrorMessage"] = "Assignment not found.";
            }

            return RedirectToPage(new { id = groupId });
        }

        private async Task LoadDataAsync()
        {
            // Get assigned modules with progress information
            var assignments = await _context.GroupModuleAssignments
                .Include(a => a.Module)
                .Where(a => a.UserGroupId == UserGroup.Id)
                .OrderBy(a => a.Module.Order)
                .ToListAsync();

            var memberCount = UserGroup.UserMemberships.Count;

            AssignedModules = new List<ModuleAssignmentInfo>();
            foreach (var assignment in assignments)
            {
                // Get completion count for this module within the group
                var completedCount = await _context.UserModuleProgress
                    .Where(p => p.ModuleId == assignment.ModuleId && 
                               p.Status == ProgressStatus.Completed && 
                               UserGroup.UserMemberships.Select(m => m.UserId).Contains(p.UserId))
                    .CountAsync();

                var completionPercentage = memberCount > 0 ? (double)completedCount / memberCount * 100 : 0;

                AssignedModules.Add(new ModuleAssignmentInfo
                {
                    Assignment = assignment,
                    MemberCount = memberCount,
                    CompletedCount = completedCount,
                    CompletionPercentage = completionPercentage
                });
            }

            // Get available modules (not yet assigned to this group)
            var assignedModuleIds = assignments.Select(a => a.ModuleId).ToList();
            AvailableModules = await _context.Modules
                .Where(m => m.IsActive && !assignedModuleIds.Contains(m.Id))
                .OrderBy(m => m.Order)
                .ToListAsync();
        }
    }
}
