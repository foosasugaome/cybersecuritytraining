using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Modules
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Module Module { get; set; } = default!;
        public IList<Lesson> Lessons { get; set; } = default!;
        public int TotalQuizzes { get; set; }
        public int AssignedUsers { get; set; }
        public int AssignedGroups { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var module = await _context.Modules
                .Include(m => m.Lessons.OrderBy(l => l.Order))
                .ThenInclude(l => l.Quizzes)
                .Include(m => m.UserAssignments)
                .Include(m => m.GroupAssignments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
            {
                return NotFound();
            }

            Module = module;
            Lessons = module.Lessons.ToList();
            TotalQuizzes = module.Lessons.Sum(l => l.Quizzes.Count);
            AssignedUsers = module.UserAssignments.Count;
            AssignedGroups = module.GroupAssignments.Count;

            return Page();
        }
    }
}
