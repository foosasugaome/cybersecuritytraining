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
            public int PassingScore { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; }
        }

        public SelectList LessonOptions { get; set; } = default!;
        public string LessonName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public int QuestionCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                LessonId = quiz.LessonId,
                PassingScore = quiz.PassingScore,
                IsActive = quiz.IsActive
            };

            LessonName = quiz.Lesson.Title;
            ModuleName = quiz.Lesson.Module.Title;
            QuestionCount = quiz.Questions.Count;

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

            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == Input.Id);

            if (quiz == null)
            {
                return NotFound();
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
                ModelState.AddModelError("Input.LessonId", "Cannot assign quiz to an inactive lesson.");
                return Page();
            }

            // Update quiz properties
            quiz.Title = Input.Title;
            quiz.Description = Input.Description;
            quiz.LessonId = Input.LessonId;
            quiz.PassingScore = Input.PassingScore;
            quiz.IsActive = Input.IsActive;
            quiz.DateModified = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Quiz updated successfully!";
                return RedirectToPage("./Details", new { id = Input.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await QuizExistsAsync(Input.Id))
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
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the quiz: {ex.Message}");
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

        private async Task<bool> QuizExistsAsync(int id)
        {
            return await _context.Quizzes.AnyAsync(e => e.Id == id);
        }
    }
}
