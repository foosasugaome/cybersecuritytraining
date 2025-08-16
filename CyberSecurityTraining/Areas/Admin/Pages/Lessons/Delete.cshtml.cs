using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Lessons
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
        public Lesson Lesson { get; set; } = default!;

        public string ModuleName { get; set; } = string.Empty;
        public int QuizCount { get; set; }
        public int UsersWithProgress { get; set; }
        public bool HasDependencies { get; set; }
        public List<string> Dependencies { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .Include(l => l.Quizzes)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            Lesson = lesson;
            ModuleName = lesson.Module.Title;
            QuizCount = lesson.Quizzes.Count;

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

            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .Include(l => l.Quizzes)
                .ThenInclude(q => q.Questions)
                .ThenInclude(q => q.Options)
                .Include(l => l.UserProgress)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            var moduleId = lesson.ModuleId;
            var lessonOrder = lesson.Order;
            var lessonTitle = lesson.Title;

            try
            {
                // The cascading deletes should handle related data automatically
                // but we'll be explicit for clarity
                
                // Remove user quiz results for quizzes in this lesson
                var quizIds = lesson.Quizzes.Select(q => q.Id).ToList();
                if (quizIds.Any())
                {
                    var userQuizResults = await _context.UserQuizResults
                        .Where(r => quizIds.Contains(r.QuizId))
                        .ToListAsync();
                    _context.UserQuizResults.RemoveRange(userQuizResults);

                    var userQuestionAnswers = await _context.UserQuestionAnswers
                        .Where(a => lesson.Quizzes.Any(q => q.Questions.Any(qu => qu.Id == a.QuestionId)))
                        .ToListAsync();
                    _context.UserQuestionAnswers.RemoveRange(userQuestionAnswers);
                }

                // Remove the lesson (cascading delete should handle quizzes, questions, options, etc.)
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();

                // Reorder remaining lessons in the module
                await ReorderLessonsAsync(moduleId, lessonOrder);

                TempData["SuccessMessage"] = $"Lesson '{lessonTitle}' has been deleted successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the lesson: {ex.Message}";
                return RedirectToPage("./Details", new { id = id });
            }
        }

        private async Task CheckDependenciesAsync(int lessonId)
        {
            Dependencies.Clear();

            // Check for user progress
            UsersWithProgress = await _context.UserLessonProgress
                .Where(p => p.LessonId == lessonId)
                .CountAsync();

            if (UsersWithProgress > 0)
            {
                Dependencies.Add($"{UsersWithProgress} user(s) have progress on this lesson");
            }

            // Check for user quiz results
            var userQuizResultsCount = await _context.UserQuizResults
                .Where(r => _context.Lessons
                    .Where(l => l.Id == lessonId)
                    .SelectMany(l => l.Quizzes)
                    .Any(q => q.Id == r.QuizId))
                .CountAsync();

            if (userQuizResultsCount > 0)
            {
                Dependencies.Add($"{userQuizResultsCount} quiz result(s) from this lesson");
            }

            // Check for user question answers
            var userAnswersCount = await _context.UserQuestionAnswers
                .Where(a => _context.Lessons
                    .Where(l => l.Id == lessonId)
                    .SelectMany(l => l.Quizzes)
                    .SelectMany(q => q.Questions)
                    .Any(qu => qu.Id == a.QuestionId))
                .CountAsync();

            if (userAnswersCount > 0)
            {
                Dependencies.Add($"{userAnswersCount} user answer(s) to questions in this lesson");
            }

            HasDependencies = Dependencies.Any();
        }

        private async Task ReorderLessonsAsync(int moduleId, int deletedOrder)
        {
            var lessonsToReorder = await _context.Lessons
                .Where(l => l.ModuleId == moduleId && l.Order > deletedOrder)
                .OrderBy(l => l.Order)
                .ToListAsync();

            foreach (var lesson in lessonsToReorder)
            {
                lesson.Order--;
            }

            if (lessonsToReorder.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
