using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Areas.Admin.Pages.Questions
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

        public Question Question { get; set; } = default!;
        public SelectList QuizOptions { get; set; } = default!;

        public class InputModel
        {
            [Required]
            [Display(Name = "Quiz")]
            public int QuizId { get; set; }

            [Required]
            [StringLength(1000, MinimumLength = 10)]
            [Display(Name = "Question Text")]
            public string Text { get; set; } = string.Empty;

            [Required]
            [Range(1, 999)]
            [Display(Name = "Order")]
            public int Order { get; set; }

            [MinLength(2, ErrorMessage = "At least 2 answer options are required")]
            [MaxLength(10, ErrorMessage = "Maximum 10 answer options allowed")]
            public List<OptionInputModel> Options { get; set; } = new();
        }

        public class OptionInputModel
        {
            [Required]
            [StringLength(500, MinimumLength = 1)]
            [Display(Name = "Option Text")]
            public string Text { get; set; } = string.Empty;

            [Display(Name = "Is Correct")]
            public bool IsCorrect { get; set; }

            [Range(1, 10)]
            public int Order { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Lesson)
                        .ThenInclude(lesson => lesson.Module)
                .Include(q => q.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            Question = question;
            await LoadQuizOptions();

            Input = new InputModel
            {
                QuizId = question.QuizId,
                Text = question.Text,
                Order = question.Order,
                Options = question.Options.OrderBy(o => o.Order).Select(o => new OptionInputModel
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    Order = o.Order
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionToUpdate = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (questionToUpdate == null)
            {
                return NotFound();
            }

            // Validate quiz exists and is accessible
            var quiz = await _context.Quizzes.FindAsync(Input.QuizId);
            if (quiz == null)
            {
                ModelState.AddModelError(nameof(Input.QuizId), "Selected quiz does not exist.");
            }

            // Validate at least one correct answer
            if (!Input.Options.Any(o => o.IsCorrect))
            {
                ModelState.AddModelError(nameof(Input.Options), "At least one answer option must be marked as correct.");
            }

            // Validate option texts are unique
            var optionTexts = Input.Options.Select(o => o.Text.Trim().ToLower()).ToList();
            if (optionTexts.Count != optionTexts.Distinct().Count())
            {
                ModelState.AddModelError(nameof(Input.Options), "Answer options must have unique text.");
            }

            // Validate order uniqueness within the quiz (excluding current question)
            if (await _context.Questions.AnyAsync(q => q.QuizId == Input.QuizId && q.Order == Input.Order && q.Id != id))
            {
                ModelState.AddModelError(nameof(Input.Order), "A question with this order already exists in the selected quiz.");
            }

            if (!ModelState.IsValid)
            {
                Question = questionToUpdate;
                await LoadQuizOptions();
                return Page();
            }

            try
            {
                // Update question properties
                questionToUpdate.QuizId = Input.QuizId;
                questionToUpdate.Text = Input.Text;
                questionToUpdate.Order = Input.Order;

                // Remove existing options
                _context.QuestionOptions.RemoveRange(questionToUpdate.Options);

                // Add new options
                questionToUpdate.Options = Input.Options.Select((option, index) => new QuestionOption
                {
                    Text = option.Text,
                    IsCorrect = option.IsCorrect,
                    Order = index + 1,
                    QuestionId = questionToUpdate.Id
                }).ToList();

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Question updated successfully.";
                return RedirectToPage("./Details", new { id = questionToUpdate.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await QuestionExists(questionToUpdate.Id))
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
                ModelState.AddModelError(string.Empty, "An error occurred while updating the question. Please try again.");
                Question = questionToUpdate;
                await LoadQuizOptions();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetQuizInfoAsync(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
            {
                return NotFound();
            }

            var nextOrder = quiz.Questions.Any() ? quiz.Questions.Max(q => q.Order) + 1 : 1;

            return new JsonResult(new
            {
                moduleTitle = quiz.Lesson.Module.Title,
                lessonTitle = quiz.Lesson.Title,
                quizTitle = quiz.Title,
                questionCount = quiz.Questions.Count,
                suggestedOrder = nextOrder,
                isActive = quiz.IsActive
            });
        }

        private async Task LoadQuizOptions()
        {
            var quizzes = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Module)
                .OrderBy(q => q.Lesson.Module.Order)
                .ThenBy(q => q.Lesson.Order)
                .ThenBy(q => q.Title)
                .Select(q => new
                {
                    q.Id,
                    DisplayText = $"{q.Lesson.Module.Title} > {q.Lesson.Title} > {q.Title}",
                    q.IsActive
                })
                .ToListAsync();

            QuizOptions = new SelectList(quizzes, "Id", "DisplayText", Input?.QuizId);
        }

        private async Task<bool> QuestionExists(int id)
        {
            return await _context.Questions.AnyAsync(e => e.Id == id);
        }
    }
}
