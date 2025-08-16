using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Lessons
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

        public SelectList ModuleSelectList { get; set; } = default!;

        public class InputModel
        {
            [Required]
            [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Lesson Title")]
            public string Title { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Lesson Content")]
            public string Content { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Module")]
            public int ModuleId { get; set; }

            [Required]
            [Range(1, 999, ErrorMessage = "Order must be between {1} and {2}")]
            [Display(Name = "Lesson Order")]
            public int Order { get; set; } = 1;

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync(int? moduleId)
        {
            await LoadModules();

            if (moduleId.HasValue)
            {
                Input.ModuleId = moduleId.Value;
                // Set default order to next available for this module
                var maxOrder = await _context.Lessons
                    .Where(l => l.ModuleId == moduleId.Value)
                    .Select(l => l.Order)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                Input.Order = maxOrder + 1;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadModules();
                return Page();
            }

            // Check for duplicate order within the same module
            if (await _context.Lessons.AnyAsync(l => l.ModuleId == Input.ModuleId && l.Order == Input.Order))
            {
                ModelState.AddModelError("Input.Order", "A lesson with this order already exists in the selected module. Please choose a different order.");
                await LoadModules();
                return Page();
            }

            var lesson = new Lesson
            {
                Title = Input.Title,
                Content = Input.Content,
                ModuleId = Input.ModuleId,
                Order = Input.Order,
                IsActive = Input.IsActive,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lesson '{lesson.Title}' has been created successfully.";
            return RedirectToPage("./Index");
        }

        private async Task LoadModules()
        {
            var modules = await _context.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .Select(m => new { m.Id, m.Title })
                .ToListAsync();

            ModuleSelectList = new SelectList(modules, "Id", "Title", Input.ModuleId);
        }
    }
}
