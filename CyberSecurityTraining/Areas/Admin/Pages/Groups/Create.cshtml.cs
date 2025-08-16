using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Groups
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        
        public List<Company> Companies { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(100)]
            [Display(Name = "Group Name")]
            public string Name { get; set; } = string.Empty;

            [StringLength(500)]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Company")]
            public int CompanyId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadData();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if group name already exists in the same company
            var existingGroup = await _context.UserGroups
                .FirstOrDefaultAsync(g => g.Name == Input.Name && g.CompanyId == Input.CompanyId);

            if (existingGroup != null)
            {
                ModelState.AddModelError("Input.Name", "A group with this name already exists in the selected company.");
                return Page();
            }

            var userGroup = new UserGroup
            {
                Name = Input.Name,
                Description = Input.Description,
                CompanyId = Input.CompanyId,
                DateCreated = DateTime.UtcNow
            };

            _context.UserGroups.Add(userGroup);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User group '{Input.Name}' has been created successfully.";
            return RedirectToPage("./Index");
        }

        private async Task LoadData()
        {
            Companies = await _context.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
