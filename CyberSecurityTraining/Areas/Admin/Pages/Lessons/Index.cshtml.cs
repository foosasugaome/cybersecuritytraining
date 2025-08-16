using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Lessons
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Lesson> Lessons { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Lessons = await _context.Lessons
                .Include(l => l.Module)
                .Include(l => l.Quizzes)
                .OrderBy(l => l.Module.Order)
                .ThenBy(l => l.Order)
                .ToListAsync();
        }
    }
}
