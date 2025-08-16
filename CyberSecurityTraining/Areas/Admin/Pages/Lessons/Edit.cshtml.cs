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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
            [Display(Name = "Lesson Title")]
            public string Title { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Content")]
            public string Content { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Module")]
            public int ModuleId { get; set; }

            [Required]
            [Range(1, 999, ErrorMessage = "Order must be between 1 and 999.")]
            [Display(Name = "Order")]
            public int Order { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; } = true;
        }

        public SelectList ModuleOptions { get; set; } = default!;
        public string ModuleName { get; set; } = string.Empty;
        public int SuggestedOrder { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Content = lesson.Content,
                ModuleId = lesson.ModuleId,
                Order = lesson.Order,
                IsActive = lesson.IsActive
            };

            ModuleName = lesson.Module.Title;
            await LoadModuleOptionsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadModuleOptionsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var lesson = await _context.Lessons.FindAsync(Input.Id);
            if (lesson == null)
            {
                return NotFound();
            }

            // Check for duplicate order within the same module (excluding current lesson)
            var duplicateOrder = await _context.Lessons
                .Where(l => l.ModuleId == Input.ModuleId && l.Order == Input.Order && l.Id != Input.Id)
                .AnyAsync();

            if (duplicateOrder)
            {
                ModelState.AddModelError("Input.Order", $"A lesson with order {Input.Order} already exists in this module.");
                return Page();
            }

            var originalModuleId = lesson.ModuleId;
            var originalOrder = lesson.Order;

            // Update lesson properties
            lesson.Title = Input.Title;
            lesson.Content = Input.Content;
            lesson.ModuleId = Input.ModuleId;
            lesson.Order = Input.Order;
            lesson.IsActive = Input.IsActive;
            lesson.DateModified = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                // If module or order changed, we might need to reorder other lessons
                if (originalModuleId != Input.ModuleId || originalOrder != Input.Order)
                {
                    await ReorderLessonsAsync(originalModuleId, Input.ModuleId);
                }

                TempData["SuccessMessage"] = "Lesson updated successfully!";
                return RedirectToPage("./Details", new { id = Input.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await LessonExistsAsync(Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the lesson: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetModuleInfoAsync(int moduleId)
        {
            var module = await _context.Modules.FindAsync(moduleId);
            if (module == null)
            {
                return NotFound();
            }

            var maxOrder = await _context.Lessons
                .Where(l => l.ModuleId == moduleId)
                .MaxAsync(l => (int?)l.Order) ?? 0;

            return new JsonResult(new
            {
                moduleName = module.Title,
                suggestedOrder = maxOrder + 1
            });
        }

        private async Task LoadModuleOptionsAsync()
        {
            var modules = await _context.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Title)
                .Select(m => new { m.Id, m.Title })
                .ToListAsync();

            ModuleOptions = new SelectList(modules, "Id", "Title", Input?.ModuleId);
        }

        private async Task ReorderLessonsAsync(int originalModuleId, int newModuleId)
        {
            // If module changed, we need to reorder lessons in both modules
            if (originalModuleId != newModuleId)
            {
                // Reorder lessons in the original module
                await ReorderLessonsInModuleAsync(originalModuleId);
                
                // Reorder lessons in the new module
                await ReorderLessonsInModuleAsync(newModuleId);
            }
            else
            {
                // Just reorder lessons in the current module
                await ReorderLessonsInModuleAsync(newModuleId);
            }
        }

        private async Task ReorderLessonsInModuleAsync(int moduleId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.ModuleId == moduleId)
                .OrderBy(l => l.Order)
                .ToListAsync();

            for (int i = 0; i < lessons.Count; i++)
            {
                lessons[i].Order = i + 1;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> LessonExistsAsync(int id)
        {
            return await _context.Lessons.AnyAsync(e => e.Id == id);
        }
    }
}
