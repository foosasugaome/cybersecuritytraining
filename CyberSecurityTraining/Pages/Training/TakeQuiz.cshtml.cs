using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Pages.Training
{
    [Authorize]
    public class TakeQuizModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TakeQuizModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Quiz Quiz { get; set; } = default!;
        public ApplicationUser CurrentUser { get; set; } = default!;
        public List<QuestionWithOptions> Questions { get; set; } = new();
        public UserQuizResult? PreviousResult { get; set; }
        public bool HasAccess { get; set; }
        public Module Module { get; set; } = default!;

        [BindProperty]
        public QuizSubmission Input { get; set; } = new();

        public class QuizSubmission
        {
            public List<UserAnswer> Answers { get; set; } = new();
        }

        public class UserAnswer
        {
            [Required]
            public int QuestionId { get; set; }
            [Required]
            public int SelectedOptionId { get; set; }
        }

        public class QuestionWithOptions
        {
            public Question Question { get; set; } = null!;
            public List<QuestionOption> Options { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            CurrentUser = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found");

            // Get the quiz with questions and options
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Module)
                .Include(q => q.Questions.Where(q => q.DateCreated != DateTime.MinValue)) // Active questions
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;
            Module = quiz.Lesson.Module;

            // Check if user has access to this quiz (through module access)
            HasAccess = await CheckQuizAccessAsync(quiz.Lesson.ModuleId);
            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You don't have access to this quiz.";
                return RedirectToPage("./Dashboard");
            }

            // Get previous quiz result
            PreviousResult = await _context.UserQuizResults
                .Where(r => r.UserId == CurrentUser.Id && r.QuizId == id)
                .OrderByDescending(r => r.CompletedAt)
                .FirstOrDefaultAsync();

            // Load questions with options (randomize option order for security)
            Questions = quiz.Questions
                .OrderBy(q => q.Order)
                .Select(q => new QuestionWithOptions
                {
                    Question = q,
                    Options = q.Options.OrderBy(o => Guid.NewGuid()).ToList() // Randomize options
                })
                .ToList();

            // Initialize answers for binding
            Input.Answers = Questions.Select(q => new UserAnswer { QuestionId = q.Question.Id }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            CurrentUser = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found");

            // Re-load quiz data
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Module)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;
            Module = quiz.Lesson.Module;

            // Check access
            if (!await CheckQuizAccessAsync(quiz.Lesson.ModuleId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await LoadQuizDataAsync();
                return Page();
            }

            // Validate all questions are answered
            if (Input.Answers.Count != Quiz.Questions.Count || Input.Answers.Any(a => a.SelectedOptionId == 0))
            {
                ModelState.AddModelError("", "Please answer all questions before submitting.");
                await LoadQuizDataAsync();
                return Page();
            }

            // Calculate score
            int correctAnswers = 0;
            var questionAnswers = new List<UserQuestionAnswer>();

            foreach (var answer in Input.Answers)
            {
                var question = Quiz.Questions.First(q => q.Id == answer.QuestionId);
                var selectedOption = question.Options.First(o => o.Id == answer.SelectedOptionId);
                bool isCorrect = selectedOption.IsCorrect;

                if (isCorrect)
                {
                    correctAnswers++;
                }

                questionAnswers.Add(new UserQuestionAnswer
                {
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId,
                    IsCorrect = isCorrect
                });
            }

            int totalQuestions = Quiz.Questions.Count;
            int scorePercentage = totalQuestions > 0 ? (correctAnswers * 100) / totalQuestions : 0;
            bool passed = scorePercentage >= Quiz.PassingScore;

            // Create quiz result
            var quizResult = new UserQuizResult
            {
                UserId = CurrentUser.Id,
                QuizId = id,
                Score = scorePercentage,
                CompletedAt = DateTime.UtcNow,
                Passed = passed
            };

            _context.UserQuizResults.Add(quizResult);
            await _context.SaveChangesAsync();

            // Add individual question answers
            foreach (var qa in questionAnswers)
            {
                qa.UserQuizResultId = quizResult.Id;
            }
            _context.UserQuestionAnswers.AddRange(questionAnswers);
            await _context.SaveChangesAsync();

            // Update lesson progress if quiz was passed
            if (passed)
            {
                var lessonProgress = await _context.UserLessonProgress
                    .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id && p.LessonId == Quiz.LessonId);

                if (lessonProgress == null)
                {
                    lessonProgress = new UserLessonProgress
                    {
                        UserId = CurrentUser.Id,
                        LessonId = Quiz.LessonId,
                        Status = ProgressStatus.Completed,
                        CompletedAt = DateTime.UtcNow
                    };
                    _context.UserLessonProgress.Add(lessonProgress);
                }
                else if (lessonProgress.Status != ProgressStatus.Completed)
                {
                    lessonProgress.Status = ProgressStatus.Completed;
                    lessonProgress.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            // Redirect to results page
            return RedirectToPage("./QuizResult", new { id = quizResult.Id });
        }

        private async Task<bool> CheckQuizAccessAsync(int moduleId)
        {
            // Check direct assignment
            var directAssignment = await _context.UserModuleAssignments
                .AnyAsync(a => a.UserId == CurrentUser.Id && a.ModuleId == moduleId);

            if (directAssignment) return true;

            // Check group assignment
            var userGroupIds = await _context.UserGroupMemberships
                .Where(m => m.UserId == CurrentUser.Id && m.IsActive)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var groupAssignment = await _context.GroupModuleAssignments
                .AnyAsync(a => userGroupIds.Contains(a.UserGroupId) && a.ModuleId == moduleId);

            return groupAssignment;
        }

        private async Task LoadQuizDataAsync()
        {
            // Reload questions and previous result for display
            Questions = Quiz.Questions
                .OrderBy(q => q.Order)
                .Select(q => new QuestionWithOptions
                {
                    Question = q,
                    Options = q.Options.OrderBy(o => Guid.NewGuid()).ToList()
                })
                .ToList();

            PreviousResult = await _context.UserQuizResults
                .Where(r => r.UserId == CurrentUser.Id && r.QuizId == Quiz.Id)
                .OrderByDescending(r => r.CompletedAt)
                .FirstOrDefaultAsync();
        }
    }
}
