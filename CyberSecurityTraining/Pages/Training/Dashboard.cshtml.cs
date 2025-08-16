using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using CyberSecurityTraining.Services;

namespace CyberSecurityTraining.Pages.Training
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProgressService _progressService;

        public DashboardModel(ApplicationDbContext context, 
                             UserManager<ApplicationUser> userManager,
                             IProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<ModuleWithProgress> AssignedModules { get; set; } = new();
        public List<UserQuizResult> RecentQuizResults { get; set; } = new();
        public int CompletedModulesCount { get; set; }
        public double OverallProgress { get; set; }
        public bool HasComprehensiveCertificate { get; set; }
        public bool AllModulesCompleted { get; set; }

        public class ModuleWithProgress
        {
            public Module Module { get; set; } = null!;
            public UserModuleProgress? Progress { get; set; }
            public double CompletionPercentage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return Challenge(); // Redirect to login page
            }

            // Get assigned modules (direct assignments and group assignments)
            var directAssignments = await _context.UserModuleAssignments
                .Where(a => a.UserId == CurrentUser.Id)
                .Select(a => a.ModuleId)
                .ToListAsync();

            // Get group assignments from all user's active groups
            var userGroupIds = await _context.UserGroupMemberships
                .Where(m => m.UserId == CurrentUser.Id && m.IsActive)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var groupAssignments = await _context.GroupModuleAssignments
                .Where(a => userGroupIds.Contains(a.UserGroupId))
                .Select(a => a.ModuleId)
                .ToListAsync();

            var allAssignedModuleIds = directAssignments.Union(groupAssignments).Distinct().ToList();

            var modules = await _context.Modules
                .Where(m => allAssignedModuleIds.Contains(m.Id) && m.IsActive)
                .OrderBy(m => m.Order)
                .ToListAsync();

            AssignedModules = new List<ModuleWithProgress>();
            
            foreach (var module in modules)
            {
                var progress = await _context.UserModuleProgress
                    .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id && p.ModuleId == module.Id);

                var completionPercentage = await _progressService.GetModuleCompletionPercentageAsync(CurrentUser.Id, module.Id);

                AssignedModules.Add(new ModuleWithProgress
                {
                    Module = module,
                    Progress = progress,
                    CompletionPercentage = completionPercentage
                });
            }

            // Get recent quiz results
            RecentQuizResults = await _context.UserQuizResults
                .Where(r => r.UserId == CurrentUser.Id)
                .Include(r => r.Quiz)
                .ThenInclude(q => q.Lesson)
                .ThenInclude(l => l.Module)
                .OrderByDescending(r => r.CompletedAt)
                .Take(5)
                .ToListAsync();

            // Calculate overall statistics
            CompletedModulesCount = AssignedModules.Count(m => m.Progress?.Status == ProgressStatus.Completed);
            
            if (AssignedModules.Any())
            {
                OverallProgress = AssignedModules.Average(m => m.CompletionPercentage);
                AllModulesCompleted = CompletedModulesCount == AssignedModules.Count;
            }

            // Check for comprehensive certificate
            HasComprehensiveCertificate = await _progressService.HasComprehensiveCertificateAsync(CurrentUser.Id);
            
            // If user doesn't have comprehensive certificate but all modules are completed, trigger the check
            if (!HasComprehensiveCertificate && AllModulesCompleted)
            {
                await _progressService.CheckAndIssueComprehensiveCertificateAsync(CurrentUser.Id);
                HasComprehensiveCertificate = await _progressService.HasComprehensiveCertificateAsync(CurrentUser.Id);
            }

            return Page();
        }
    }
}
