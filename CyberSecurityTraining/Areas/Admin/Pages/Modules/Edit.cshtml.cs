using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Modules
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

        public Module Module { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Module Title")]
            public string Title { get; set; } = string.Empty;

            [StringLength(1000, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Required]
            [Range(1, 999, ErrorMessage = "Order must be between {1} and {2}")]
            [Display(Name = "Display Order")]
            public int Order { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == id);
            if (module == null)
            {
                return NotFound();
            }

            Module = module;
            Input = new InputModel
            {
                Id = module.Id,
                Title = module.Title,
                Description = module.Description,
                Order = module.Order,
                IsActive = module.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == Input.Id);
                return Page();
            }

            // Check for duplicate order (excluding current module)
            if (_context.Modules.Any(m => m.Order == Input.Order && m.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Order", "A module with this order already exists. Please choose a different order.");
                Module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == Input.Id);
                return Page();
            }

            var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == Input.Id);
            if (module == null)
            {
                return NotFound();
            }

            module.Title = Input.Title;
            module.Description = Input.Description;
            module.Order = Input.Order;
            module.IsActive = Input.IsActive;
            module.DateModified = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Module '{module.Title}' has been updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ModuleExists(Input.Id))
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

        private bool ModuleExists(int id)
        {
            return _context.Modules.Any(e => e.Id == id);
        }
    }
}
