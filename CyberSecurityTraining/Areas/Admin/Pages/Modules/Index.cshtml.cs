using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Modules
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Module> Modules { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Modules = await _context.Modules
                .Include(m => m.Lessons)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Title)
                .ToListAsync();
        }
    }
}
