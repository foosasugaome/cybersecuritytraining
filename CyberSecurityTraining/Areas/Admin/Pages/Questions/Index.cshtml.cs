using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Questions
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Question> Questions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Questions = await _context.Questions
                .Include(q => q.Quiz)
                .ThenInclude(quiz => quiz.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Options)
                .OrderBy(q => q.Quiz.Lesson.Module.Order)
                .ThenBy(q => q.Quiz.Lesson.Order)
                .ThenBy(q => q.Quiz.Title)
                .ThenBy(q => q.Order)
                .ToListAsync();
        }
    }
}
