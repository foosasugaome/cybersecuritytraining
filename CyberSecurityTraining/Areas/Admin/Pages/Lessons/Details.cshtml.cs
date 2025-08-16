using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using Markdig;

namespace CyberSecurityTraining.Areas.Admin.Pages.Lessons
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Lesson Lesson { get; set; } = default!;
        public string RenderedContent { get; set; } = string.Empty;
        public IList<Quiz> Quizzes { get; set; } = default!;
        public int TotalQuestions { get; set; }
        public int UsersWithProgress { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .Include(l => l.Quizzes.OrderBy(q => q.Title))
                .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            Lesson = lesson;
            Quizzes = lesson.Quizzes.ToList();
            TotalQuestions = lesson.Quizzes.Sum(q => q.Questions.Count);
            
            // Count users with progress on this lesson
            UsersWithProgress = await _context.UserLessonProgress
                .Where(p => p.LessonId == id.Value)
                .CountAsync();

            // Render Markdown content to HTML
            RenderedContent = Markdown.ToHtml(lesson.Content ?? "");

            return Page();
        }
    }
}
