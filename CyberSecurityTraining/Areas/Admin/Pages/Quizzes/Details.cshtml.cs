using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Quizzes
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Quiz Quiz { get; set; } = default!;
        public IList<Question> Questions { get; set; } = default!;
        public int TotalQuestions { get; set; }
        public int TotalOptions { get; set; }
        public int UsersAttempted { get; set; }
        public int UsersCompleted { get; set; }
        public double AverageScore { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Questions.OrderBy(qu => qu.Order))
                .ThenInclude(qu => qu.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;
            Questions = quiz.Questions.ToList();
            TotalQuestions = quiz.Questions.Count;
            TotalOptions = quiz.Questions.Sum(q => q.Options.Count);

            // Get user statistics
            await LoadQuizStatisticsAsync(id.Value);

            return Page();
        }

        private async Task LoadQuizStatisticsAsync(int quizId)
        {
            var results = await _context.UserQuizResults
                .Where(r => r.QuizId == quizId)
                .ToListAsync();

            UsersAttempted = results.Count;
            UsersCompleted = results.Count; // All results in the table are completed
            
            if (results.Any())
            {
                AverageScore = results.Average(r => r.Score);
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
            {
                return NotFound();
            }

            quiz.IsActive = !quiz.IsActive;
            quiz.DateModified = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Quiz status updated to {(quiz.IsActive ? "Active" : "Inactive")}.";
            return RedirectToPage(new { id = id });
        }
    }
}
