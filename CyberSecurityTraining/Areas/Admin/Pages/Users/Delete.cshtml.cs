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
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser User { get; set; } = null!;
        public List<UserGroupMembership> GroupMemberships { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;

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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? id)
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

            try
            {
                // Remove all group memberships first
                var memberships = await _context.UserGroupMemberships
                    .Where(m => m.UserId == id)
                    .ToListAsync();

                _context.UserGroupMemberships.RemoveRange(memberships);

                // Remove user progress and other related data
                var moduleProgress = await _context.UserModuleProgress
                    .Where(p => p.UserId == id)
                    .ToListAsync();
                _context.UserModuleProgress.RemoveRange(moduleProgress);

                var lessonProgress = await _context.UserLessonProgress
                    .Where(p => p.UserId == id)
                    .ToListAsync();
                _context.UserLessonProgress.RemoveRange(lessonProgress);

                var quizResults = await _context.UserQuizResults
                    .Where(r => r.UserId == id)
                    .ToListAsync();
                _context.UserQuizResults.RemoveRange(quizResults);

                var questionAnswers = await _context.UserQuestionAnswers
                    .Where(a => a.UserQuizResult.UserId == id)
                    .ToListAsync();
                _context.UserQuestionAnswers.RemoveRange(questionAnswers);

                var moduleAssignments = await _context.UserModuleAssignments
                    .Where(a => a.UserId == id)
                    .ToListAsync();
                _context.UserModuleAssignments.RemoveRange(moduleAssignments);

                await _context.SaveChangesAsync();

                // Finally, delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User '{user.FirstName} {user.LastName}' has been deleted successfully.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while deleting the user: {ex.Message}";
            }

            // If we got here, something failed, reload the page
            await OnGetAsync(id);
            return Page();
        }
    }
}
