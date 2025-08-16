using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Modules
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
        public Module Module { get; set; } = default!;

        public int LessonCount { get; set; }
        public int QuizCount { get; set; }
        public int AssignedUsers { get; set; }
        public int AssignedGroups { get; set; }
        public bool CanDelete { get; set; }
        public List<string> DeletionWarnings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var module = await _context.Modules
                .Include(m => m.Lessons)
                .ThenInclude(l => l.Quizzes)
                .Include(m => m.UserAssignments)
                .Include(m => m.GroupAssignments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
            {
                return NotFound();
            }

            Module = module;
            LessonCount = module.Lessons.Count;
            QuizCount = module.Lessons.Sum(l => l.Quizzes.Count);
            AssignedUsers = module.UserAssignments.Count;
            AssignedGroups = module.GroupAssignments.Count;

            // Determine if module can be safely deleted
            CanDelete = LessonCount == 0 && AssignedUsers == 0 && AssignedGroups == 0;

            // Generate warnings
            if (LessonCount > 0)
            {
                DeletionWarnings.Add($"This module has {LessonCount} lesson(s) with {QuizCount} quiz(es). All lessons and quizzes will be permanently deleted.");
            }
            if (AssignedUsers > 0)
            {
                DeletionWarnings.Add($"This module is assigned to {AssignedUsers} user(s). User progress will be lost.");
            }
            if (AssignedGroups > 0)
            {
                DeletionWarnings.Add($"This module is assigned to {AssignedGroups} group(s). Group assignments will be removed.");
            }
            if (LessonCount > 0 || AssignedUsers > 0 || AssignedGroups > 0)
            {
                DeletionWarnings.Add("This action cannot be undone. Consider deactivating the module instead.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
            {
                return NotFound();
            }

            var moduleTitle = module.Title;

            try
            {
                // Remove the module - cascade deletes should handle related entities
                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Module '{moduleTitle}' and all associated content has been deleted successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting module: {ex.Message}";
                return RedirectToPage("./Delete", new { id });
            }
        }
    }
}
