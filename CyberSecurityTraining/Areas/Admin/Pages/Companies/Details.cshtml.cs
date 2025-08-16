using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Companies
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Company Company { get; set; } = default!;
        public IList<ApplicationUser> Users { get; set; } = default!;
        public IList<UserGroup> UserGroups { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .Include(c => c.Users)
                .Include(c => c.UserGroups)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            Company = company;
            Users = company.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
            UserGroups = company.UserGroups.OrderBy(g => g.Name).ToList();

            return Page();
        }
    }
}
