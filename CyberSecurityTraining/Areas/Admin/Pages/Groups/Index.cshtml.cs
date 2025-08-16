using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<UserGroup> UserGroups { get; set; } = default!;

        public async Task OnGetAsync()
        {
            UserGroups = await _context.UserGroups
                .Include(g => g.Company)
                .Include(g => g.UserMemberships)
                    .ThenInclude(m => m.User)
                .OrderBy(g => g.Company.Name)
                .ThenBy(g => g.Name)
                .ToListAsync();
        }
    }
}
