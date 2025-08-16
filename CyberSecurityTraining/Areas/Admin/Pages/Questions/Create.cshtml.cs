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
            [Display(Name = "Question Text")]
            public string Text { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Quiz")]
            public int QuizId { get; set; }

            [Required]
            [Range(1, 999, ErrorMessage = "Order must be between 1 and 999.")]
            [Display(Name = "Order")]
            public int Order { get; set; }

            [Display(Name = "Answer Options")]
            [MinLength(2, ErrorMessage = "At least 2 answer options are required.")]
            [MaxLength(10, ErrorMessage = "Maximum 10 answer options are allowed.")]
            public List<OptionInputModel> Options { get; set; } = new List<OptionInputModel>();
        }

        public class OptionInputModel
        {
            [Required]
            [Display(Name = "Option Text")]
            public string Text { get; set; } = string.Empty;

            [Display(Name = "Is Correct")]
            public bool IsCorrect { get; set; }

            public int Order { get; set; }
        }

        public SelectList QuizOptions { get; set; } = default!;
        public string? PreselectedQuizId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? quizId)
        {
            PreselectedQuizId = quizId?.ToString();
            
            if (quizId.HasValue)
            {
                // Verify the quiz exists
                var quizExists = await _context.Quizzes.AnyAsync(q => q.Id == quizId.Value);
                if (!quizExists)
                {
                    TempData["ErrorMessage"] = "The specified quiz was not found.";
                    return RedirectToPage("./Index");
                }
                
                Input = new InputModel
                {
                    QuizId = quizId.Value,
                    Order = await GetSuggestedOrderAsync(quizId.Value),
                    Options = new List<OptionInputModel>
                    {
                        new OptionInputModel { Order = 1, IsCorrect = true },
                        new OptionInputModel { Order = 2, IsCorrect = false },
                        new OptionInputModel { Order = 3, IsCorrect = false },
                        new OptionInputModel { Order = 4, IsCorrect = false }
                    }
                };
            }
            else
            {
                Input = new InputModel
                {
                    Order = 1,
                    Options = new List<OptionInputModel>
                    {
                        new OptionInputModel { Order = 1, IsCorrect = true },
                        new OptionInputModel { Order = 2, IsCorrect = false },
                        new OptionInputModel { Order = 3, IsCorrect = false },
                        new OptionInputModel { Order = 4, IsCorrect = false }
                    }
                };
            }

            await LoadQuizOptionsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadQuizOptionsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validate that at least one option is marked as correct
            if (!Input.Options.Any(o => o.IsCorrect))
            {
                ModelState.AddModelError("Input.Options", "At least one answer option must be marked as correct.");
                return Page();
            }

            // Check for duplicate order within the same quiz
            var duplicateOrder = await _context.Questions
                .Where(q => q.QuizId == Input.QuizId && q.Order == Input.Order)
                .AnyAsync();

            if (duplicateOrder)
            {
                ModelState.AddModelError("Input.Order", $"A question with order {Input.Order} already exists in this quiz.");
                return Page();
            }

            // Verify the quiz exists and is active
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(q => q.Id == Input.QuizId);

            if (quiz == null)
            {
                ModelState.AddModelError("Input.QuizId", "The selected quiz was not found.");
                return Page();
            }

            var question = new Question
            {
                Text = Input.Text,
                QuizId = Input.QuizId,
                Order = Input.Order,
                DateCreated = DateTime.UtcNow
            };

            // Add options
            for (int i = 0; i < Input.Options.Count; i++)
            {
                var option = Input.Options[i];
                if (!string.IsNullOrWhiteSpace(option.Text))
                {
                    question.Options.Add(new QuestionOption
                    {
                        Text = option.Text,
                        IsCorrect = option.IsCorrect,
                        Order = i + 1
                    });
                }
            }

            try
            {
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Question has been created successfully with {question.Options.Count} answer options!";
                return RedirectToPage("./Details", new { id = question.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the question: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetQuizInfoAsync(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
            {
                return NotFound();
            }

            var questionCount = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .CountAsync();

            var suggestedOrder = await GetSuggestedOrderAsync(quizId);

            return new JsonResult(new
            {
                quizTitle = quiz.Title,
                lessonTitle = quiz.Lesson.Title,
                moduleTitle = quiz.Lesson.Module.Title,
                questionCount = questionCount,
                suggestedOrder = suggestedOrder,
                isActive = quiz.IsActive
            });
        }

        private async Task LoadQuizOptionsAsync()
        {
            var quizzes = await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .Where(q => q.IsActive && q.Lesson.IsActive && q.Lesson.Module.IsActive)
                .OrderBy(q => q.Lesson.Module.Order)
                .ThenBy(q => q.Lesson.Order)
                .ThenBy(q => q.Title)
                .Select(q => new { 
                    q.Id, 
                    DisplayText = $"{q.Lesson.Module.Title} > {q.Lesson.Title} > {q.Title}"
                })
                .ToListAsync();

            QuizOptions = new SelectList(quizzes, "Id", "DisplayText", Input?.QuizId);
        }

        private async Task<int> GetSuggestedOrderAsync(int quizId)
        {
            var maxOrder = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .MaxAsync(q => (int?)q.Order) ?? 0;

            return maxOrder + 1;
        }
    }
}
