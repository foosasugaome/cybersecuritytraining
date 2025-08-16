using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Quizzes
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Quiz> Quizzes { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Quizzes = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                .OrderBy(q => q.Lesson.Module.Order)
                .ThenBy(q => q.Lesson.Order)
                .ThenBy(q => q.Title)
                .ToListAsync();
        }
    }
}
