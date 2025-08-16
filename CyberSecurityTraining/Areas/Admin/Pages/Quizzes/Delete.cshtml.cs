using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Quizzes
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
        public Quiz Quiz { get; set; } = default!;

        public string ModuleName { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int OptionCount { get; set; }
        public int UserAttempts { get; set; }
        public bool HasDependencies { get; set; }
        public List<string> Dependencies { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                .ThenInclude(qu => qu.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;
            ModuleName = quiz.Lesson.Module.Title;
            LessonName = quiz.Lesson.Title;
            QuestionCount = quiz.Questions.Count;
            OptionCount = quiz.Questions.Sum(q => q.Options.Count);

            // Check for dependencies
            await CheckDependenciesAsync(id.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                .ThenInclude(qu => qu.Options)
                .Include(q => q.UserResults)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            var quizTitle = quiz.Title;

            try
            {
                // The cascading deletes should handle related data automatically
                // but we'll be explicit for clarity
                
                // Remove user question answers for questions in this quiz
                var questionIds = quiz.Questions.Select(q => q.Id).ToList();
                if (questionIds.Any())
                {
                    var userQuestionAnswers = await _context.UserQuestionAnswers
                        .Where(a => questionIds.Contains(a.QuestionId))
                        .ToListAsync();
                    _context.UserQuestionAnswers.RemoveRange(userQuestionAnswers);
                }

                // Remove the quiz (cascading delete should handle questions, options, user results, etc.)
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Quiz '{quizTitle}' has been deleted successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the quiz: {ex.Message}";
                return RedirectToPage("./Details", new { id = id });
            }
        }

        private async Task CheckDependenciesAsync(int quizId)
        {
            Dependencies.Clear();

            // Check for user quiz results
            UserAttempts = await _context.UserQuizResults
                .Where(r => r.QuizId == quizId)
                .CountAsync();

            if (UserAttempts > 0)
            {
                Dependencies.Add($"{UserAttempts} user quiz result(s)");
            }

            // Check for user question answers
            var questionIds = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Select(q => q.Id)
                .ToListAsync();

            if (questionIds.Any())
            {
                var userAnswersCount = await _context.UserQuestionAnswers
                    .Where(a => questionIds.Contains(a.QuestionId))
                    .CountAsync();

                if (userAnswersCount > 0)
                {
                    Dependencies.Add($"{userAnswersCount} user answer(s) to questions in this quiz");
                }
            }

            HasDependencies = Dependencies.Any();
        }
    }
}
