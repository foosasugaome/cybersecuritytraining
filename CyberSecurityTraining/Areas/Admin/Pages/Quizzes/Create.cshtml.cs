using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Quizzes
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
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            [Required]
            [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
            [Display(Name = "Quiz Title")]
            public string Title { get; set; } = string.Empty;

            [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Lesson")]
            public int LessonId { get; set; }

            [Required]
            [Range(1, 100, ErrorMessage = "Passing score must be between 1 and 100.")]
            [Display(Name = "Passing Score (%)")]
            public int PassingScore { get; set; } = 70;

            [Display(Name = "Active")]
            public bool IsActive { get; set; } = true;
        }

        public SelectList LessonOptions { get; set; } = default!;
        public string? PreselectedLessonId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? lessonId)
        {
            PreselectedLessonId = lessonId?.ToString();
            
            if (lessonId.HasValue)
            {
                // Verify the lesson exists
                var lessonExists = await _context.Lessons.AnyAsync(l => l.Id == lessonId.Value);
                if (!lessonExists)
                {
                    TempData["ErrorMessage"] = "The specified lesson was not found.";
                    return RedirectToPage("./Index");
                }
                
                Input = new InputModel
                {
                    LessonId = lessonId.Value,
                    PassingScore = 70,
                    IsActive = true
                };
            }
            else
            {
                Input = new InputModel
                {
                    PassingScore = 70,
                    IsActive = true
                };
            }

            await LoadLessonOptionsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadLessonOptionsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Verify the lesson exists and is active
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == Input.LessonId);

            if (lesson == null)
            {
                ModelState.AddModelError("Input.LessonId", "The selected lesson was not found.");
                return Page();
            }

            if (!lesson.IsActive)
            {
                ModelState.AddModelError("Input.LessonId", "Cannot create a quiz for an inactive lesson.");
                return Page();
            }

            var quiz = new Quiz
            {
                Title = Input.Title,
                Description = Input.Description,
                LessonId = Input.LessonId,
                PassingScore = Input.PassingScore,
                IsActive = Input.IsActive,
                DateCreated = DateTime.UtcNow
            };

            try
            {
                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Quiz '{quiz.Title}' has been created successfully!";
                return RedirectToPage("./Details", new { id = quiz.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the quiz: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetLessonInfoAsync(int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound();
            }

            var quizCount = await _context.Quizzes
                .Where(q => q.LessonId == lessonId)
                .CountAsync();

            return new JsonResult(new
            {
                lessonTitle = lesson.Title,
                moduleTitle = lesson.Module.Title,
                quizCount = quizCount,
                isActive = lesson.IsActive
            });
        }

        private async Task LoadLessonOptionsAsync()
        {
            var lessons = await _context.Lessons
                .Include(l => l.Module)
                .Where(l => l.IsActive && l.Module.IsActive)
                .OrderBy(l => l.Module.Order)
                .ThenBy(l => l.Order)
                .ThenBy(l => l.Title)
                .Select(l => new { 
                    l.Id, 
                    DisplayText = $"{l.Module.Title} > {l.Title} (Order: {l.Order})"
                })
                .ToListAsync();

            LessonOptions = new SelectList(lessons, "Id", "DisplayText", Input?.LessonId);
        }
    }
}
