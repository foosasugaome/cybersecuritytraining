using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public UserGroup UserGroup { get; set; } = default!;
        public List<Company> Companies { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(100)]
            [Display(Name = "Group Name")]
            public string Name { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Company")]
            public int CompanyId { get; set; }

            [StringLength(500)]
            [Display(Name = "Description")]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.UserGroups == null)
            {
                return NotFound();
            }

            var userGroup = await _context.UserGroups
                .Include(g => g.Company)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userGroup == null)
            {
                return NotFound();
            }

            UserGroup = userGroup;

            Input = new InputModel
            {
                Name = userGroup.Name,
                CompanyId = userGroup.CompanyId,
                Description = userGroup.Description
            };

            await LoadCompaniesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var userGroup = await _context.UserGroups
                    .Include(g => g.Company)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (userGroup == null)
                {
                    return NotFound();
                }

                UserGroup = userGroup;
                await LoadCompaniesAsync();
                return Page();
            }

            var groupToUpdate = await _context.UserGroups.FindAsync(id);

            if (groupToUpdate == null)
            {
                return NotFound();
            }

            // Check if name already exists for another group in the same company
            var existingGroup = await _context.UserGroups
                .FirstOrDefaultAsync(g => g.Id != id && 
                                   g.CompanyId == Input.CompanyId && 
                                   g.Name == Input.Name);

            if (existingGroup != null)
            {
                ModelState.AddModelError("Input.Name", "A group with this name already exists in the selected company.");
                
                UserGroup = await _context.UserGroups
                    .Include(g => g.Company)
                    .FirstOrDefaultAsync(m => m.Id == id) ?? groupToUpdate;
                
                await LoadCompaniesAsync();
                return Page();
            }

            // Validate company exists
            var company = await _context.Companies.FindAsync(Input.CompanyId);
            if (company == null)
            {
                ModelState.AddModelError("Input.CompanyId", "Selected company does not exist.");
                
                UserGroup = await _context.UserGroups
                    .Include(g => g.Company)
                    .FirstOrDefaultAsync(m => m.Id == id) ?? groupToUpdate;
                
                await LoadCompaniesAsync();
                return Page();
            }

            // Update the group
            groupToUpdate.Name = Input.Name;
            groupToUpdate.CompanyId = Input.CompanyId;
            groupToUpdate.Description = Input.Description;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Group '{groupToUpdate.Name}' has been successfully updated.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserGroupExists(id.Value))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task LoadCompaniesAsync()
        {
            Companies = await _context.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        private bool UserGroupExists(int id)
        {
            return (_context.UserGroups?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
