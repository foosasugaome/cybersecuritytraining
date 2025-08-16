using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Areas.Admin.Pages.Companies
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Company Company { get; set; } = default!;

        public int UserCount { get; set; }
        public int GroupCount { get; set; }
        public bool CanDelete { get; set; }
        public List<string> DeletionWarnings { get; set; } = new();

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
            UserCount = company.Users.Count;
            GroupCount = company.UserGroups.Count;

            // Determine if company can be safely deleted
            CanDelete = UserCount == 0 && GroupCount == 0;

            // Generate warnings
            if (UserCount > 0)
            {
                DeletionWarnings.Add($"This company has {UserCount} user(s) associated with it.");
            }
            if (GroupCount > 0)
            {
                DeletionWarnings.Add($"This company has {GroupCount} user group(s) associated with it.");
            }
            if (!CanDelete)
            {
                DeletionWarnings.Add("Please remove or reassign all users and groups before deleting this company.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
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

            // Double-check that company can be deleted
            if (company.Users.Any() || company.UserGroups.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete company. Please remove all associated users and groups first.";
                return RedirectToPage("./Delete", new { id });
            }

            var companyName = company.Name;
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Company '{companyName}' has been deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
