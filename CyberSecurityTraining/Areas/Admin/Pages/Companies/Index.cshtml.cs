using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Companies
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Company> Companies { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Companies = await _context.Companies
                .Include(c => c.Users)
                .Include(c => c.UserGroups)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
