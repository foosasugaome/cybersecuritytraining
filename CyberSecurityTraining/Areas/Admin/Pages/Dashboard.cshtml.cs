using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalModules { get; set; }
        public int TotalGroups { get; set; }
        public List<Company> RecentCompanies { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalUsers = await _context.Users.CountAsync();
            TotalCompanies = await _context.Companies.CountAsync();
            TotalModules = await _context.Modules.CountAsync();
            TotalGroups = await _context.UserGroups.CountAsync();

            RecentCompanies = await _context.Companies
                .OrderByDescending(c => c.DateCreated)
                .Take(5)
                .ToListAsync();

            RecentUsers = await _context.Users
                .OrderByDescending(u => u.DateCreated)
                .Take(5)
                .ToListAsync();
        }
    }
}
