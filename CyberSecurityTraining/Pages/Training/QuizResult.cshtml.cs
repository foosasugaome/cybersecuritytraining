using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using Microsoft.AspNetCore.Identity;

namespace CyberSecurityTraining.Pages.Training
{
    public class QuizResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizResultModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public UserQuizResult QuizResult { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;
        public Module Module { get; set; } = null!;
        public List<QuestionReview> QuestionReviews { get; set; } = new();

        public class QuestionReview
        {
            public Question Question { get; set; } = null!;
            public List<QuestionOption> Options { get; set; } = new();
            public QuestionOption? CorrectOption { get; set; }
            public QuestionOption? SelectedOption { get; set; }
            public bool IsCorrect { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Load the quiz result with related data
            QuizResult = await _context.UserQuizResults
                .Include(r => r.Quiz)
                    .ThenInclude(q => q.Lesson)
                .Include(r => r.Quiz)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(q => q.Options)
                .Include(r => r.Answers)
                    .ThenInclude(a => a.Question)
                .Include(r => r.Answers)
                    .ThenInclude(a => a.SelectedOption)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (QuizResult == null)
            {
                return NotFound();
            }

            Quiz = QuizResult.Quiz;

            // Get the module this quiz belongs to
            Module = await _context.Modules
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(m => m.Lessons.Any(l => l.Quizzes.Any(q => q.Id == Quiz.Id)));

            if (Module == null)
            {
                return NotFound();
            }

            // Check access to this module
            var hasAccess = await CheckModuleAccessAsync(user.Id, Module.Id);
            if (!hasAccess)
            {
                return Forbid();
            }

            // Build question reviews
            await BuildQuestionReviewsAsync();

            return Page();
        }

        private async Task<bool> CheckModuleAccessAsync(string userId, int moduleId)
        {
            // Check if user has access through group assignments
            var hasGroupAccess = await _context.GroupModuleAssignments
                .Where(gma => gma.ModuleId == moduleId)
                .AnyAsync(gma => gma.UserGroup.UserMemberships
                    .Any(ugm => ugm.UserId == userId));

            // Check if user has direct assignment
            var hasDirectAccess = await _context.UserModuleAssignments
                .AnyAsync(uma => uma.UserId == userId && uma.ModuleId == moduleId);

            return hasGroupAccess || hasDirectAccess;
        }

        private async Task BuildQuestionReviewsAsync()
        {
            QuestionReviews = new List<QuestionReview>();

            foreach (var question in Quiz.Questions.OrderBy(q => q.Id))
            {
                var options = question.Options.OrderBy(o => o.Id).ToList();
                var correctOption = options.FirstOrDefault(o => o.IsCorrect);
                var userAnswer = QuizResult.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var selectedOption = userAnswer?.SelectedOption;

                var review = new QuestionReview
                {
                    Question = question,
                    Options = options,
                    CorrectOption = correctOption,
                    SelectedOption = selectedOption,
                    IsCorrect = selectedOption != null && selectedOption.IsCorrect
                };

                QuestionReviews.Add(review);
            }
        }

        public async Task<IActionResult> OnPostRetakeQuizAsync(int resultId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await _context.UserQuizResults
                .Include(r => r.Quiz)
                    .ThenInclude(q => q.Lesson)
                .FirstOrDefaultAsync(r => r.Id == resultId && r.UserId == user.Id);

            if (result == null)
            {
                return NotFound();
            }

            // Get the module
            var module = await _context.Modules
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(m => m.Lessons.Any(l => l.Quizzes.Any(q => q.Id == result.Quiz.Id)));

            if (module == null)
            {
                return NotFound();
            }

            // Redirect to take quiz again
            return RedirectToPage("/Training/TakeQuiz", new { 
                id = result.Quiz.Id 
            });
        }
    }
}
