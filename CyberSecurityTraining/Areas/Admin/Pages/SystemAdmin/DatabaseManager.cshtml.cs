using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Services;

namespace CyberSecurityTraining.Areas.Admin.Pages.SystemAdmin
{
    [Authorize(Roles = "Admin")]
    public class DatabaseManagerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public DatabaseManagerModel(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public DatabaseStats DatabaseStats { get; set; } = new DatabaseStats();
        public bool? IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Double-check admin authorization
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await LoadDatabaseStatsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Triple-check admin authorization for POST
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            try
            {
                // Call the seed data service to clear and re-seed
                await SeedData.InitializeAsync(_serviceProvider);
                
                IsSuccess = true;
                await LoadDatabaseStatsAsync();
                
                return Page();
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                ErrorMessage = ex.Message;
                await LoadDatabaseStatsAsync();
                
                return Page();
            }
        }

        private async Task LoadDatabaseStatsAsync()
        {
            try
            {
                DatabaseStats = new DatabaseStats
                {
                    CompanyCount = await _context.Companies.CountAsync(),
                    UserGroupCount = await _context.UserGroups.CountAsync(),
                    UserCount = await _context.Users.CountAsync(),
                    ModuleCount = await _context.Modules.CountAsync(),
                    LessonCount = await _context.Lessons.CountAsync(),
                    QuizCount = await _context.Quizzes.CountAsync(),
                    QuestionCount = await _context.Questions.CountAsync()
                };
            }
            catch (Exception)
            {
                // If there's an error loading stats, initialize with zeros
                DatabaseStats = new DatabaseStats();
            }
        }
    }

    public class DatabaseStats
    {
        public int CompanyCount { get; set; }
        public int UserGroupCount { get; set; }
        public int UserCount { get; set; }
        public int ModuleCount { get; set; }
        public int LessonCount { get; set; }
        public int QuizCount { get; set; }
        public int QuestionCount { get; set; }
    }
}
