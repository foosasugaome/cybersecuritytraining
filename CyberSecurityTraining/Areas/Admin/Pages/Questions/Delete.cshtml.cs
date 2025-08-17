using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Questions
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
        public Question Question { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Lesson)
                        .ThenInclude(lesson => lesson.Module)
                .Include(q => q.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (question == null)
            {
                return NotFound();
            }
            else
            {
                Question = question;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Options)
                .Include(q => q.UserAnswers)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question != null)
            {
                try
                {
                    // Check if there are any student responses
                    var hasStudentResponses = question.UserAnswers.Any();
                    
                    if (hasStudentResponses)
                    {
                        TempData["ErrorMessage"] = "Cannot delete question because it has student responses. Consider deactivating the quiz instead.";
                        return RedirectToPage("./Details", new { id = question.Id });
                    }

                    // Remove all question options first
                    _context.QuestionOptions.RemoveRange(question.Options);
                    
                    // Remove the question
                    _context.Questions.Remove(question);
                    
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Question deleted successfully.";
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while deleting the question. Please try again.";
                    return RedirectToPage("./Details", new { id = question.Id });
                }
            }

            return RedirectToPage("./Index");
        }
    }
}
