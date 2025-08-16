using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Companies
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
        public InputModel Input { get; set; } = new();

        public Company Company { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Company Name")]
            public string Name { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [Display(Name = "Description")]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FirstOrDefaultAsync(m => m.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            Company = company;
            Input = new InputModel
            {
                Id = company.Id,
                Name = company.Name,
                Description = company.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Company = await _context.Companies.FirstOrDefaultAsync(m => m.Id == Input.Id);
                return Page();
            }

            var company = await _context.Companies.FirstOrDefaultAsync(m => m.Id == Input.Id);
            if (company == null)
            {
                return NotFound();
            }

            company.Name = Input.Name;
            company.Description = Input.Description;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Company '{company.Name}' has been updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyExists(Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Details", new { id = Input.Id });
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}
