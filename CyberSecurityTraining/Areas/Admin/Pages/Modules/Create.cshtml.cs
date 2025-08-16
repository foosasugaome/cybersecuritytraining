using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Modules
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

        public class InputModel
        {
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
            public int Order { get; set; } = 1;

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            // Set default order to next available
            var maxOrder = _context.Modules.Any() ? _context.Modules.Max(m => m.Order) : 0;
            Input.Order = maxOrder + 1;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check for duplicate order
            if (_context.Modules.Any(m => m.Order == Input.Order))
            {
                ModelState.AddModelError("Input.Order", "A module with this order already exists. Please choose a different order.");
                return Page();
            }

            var module = new Module
            {
                Title = Input.Title,
                Description = Input.Description,
                Order = Input.Order,
                IsActive = Input.IsActive,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Module '{module.Title}' has been created successfully.";
            return RedirectToPage("./Index");
        }
    }
}
