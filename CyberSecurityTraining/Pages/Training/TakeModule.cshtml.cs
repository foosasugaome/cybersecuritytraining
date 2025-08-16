using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using CyberSecurityTraining.Services;

namespace CyberSecurityTraining.Pages.Training
{
    [Authorize]
    public class TakeModuleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProgressService _progressService;

        public TakeModuleModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
        }

        public Module Module { get; set; } = default!;
        public ApplicationUser CurrentUser { get; set; } = default!;
        public List<LessonInfo> Lessons { get; set; } = new();
        public UserModuleProgress? ModuleProgress { get; set; }
        public int CurrentLessonIndex { get; set; }
        public bool HasAccess { get; set; }
        public double CompletionPercentage { get; set; }

        public class LessonInfo
        {
            public Lesson Lesson { get; set; } = null!;
            public UserLessonProgress? Progress { get; set; }
            public bool IsUnlocked { get; set; }
            public List<Quiz> Quizzes { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(int? id, int? lessonIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            CurrentUser = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found");

            // Get the module with lessons
            var module = await _context.Modules
                .Include(m => m.Lessons.Where(l => l.IsActive))
                    .ThenInclude(l => l.Quizzes.Where(q => q.IsActive))
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (module == null)
            {
                return NotFound();
            }

            Module = module;

            // Check if user has access to this module
            HasAccess = await CheckModuleAccessAsync(id.Value);
            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You don't have access to this training module.";
                return RedirectToPage("./Dashboard");
            }

            // Get or create module progress
            ModuleProgress = await _context.UserModuleProgress
                .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id && p.ModuleId == id);

            if (ModuleProgress == null)
            {
                ModuleProgress = new UserModuleProgress
                {
                    UserId = CurrentUser.Id,
                    ModuleId = id.Value,
                    Status = ProgressStatus.NotStarted
                };
                _context.UserModuleProgress.Add(ModuleProgress);
            }

            // Start the module if not started
            if (ModuleProgress.Status == ProgressStatus.NotStarted)
            {
                ModuleProgress.Status = ProgressStatus.InProgress;
                ModuleProgress.StartedAt = DateTime.UtcNow;
            }

            ModuleProgress.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Load lessons with progress
            await LoadLessonsAsync();

            // Set current lesson index
            CurrentLessonIndex = lessonIndex ?? GetNextIncompleteLesson();

            // Calculate completion percentage
            CompletionPercentage = await _progressService.GetModuleCompletionPercentageAsync(CurrentUser.Id, id.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostMarkLessonCompleteAsync(int moduleId, int lessonId)
        {
            CurrentUser = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found");

            // Load the module first
            var module = await _context.Modules
                .Include(m => m.Lessons.Where(l => l.IsActive))
                    .ThenInclude(l => l.Quizzes.Where(q => q.IsActive))
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive);

            if (module == null)
            {
                return NotFound();
            }

            Module = module;

            // Verify access
            if (!await CheckModuleAccessAsync(moduleId))
            {
                return Forbid();
            }

            // Get or create lesson progress
            var lessonProgress = await _context.UserLessonProgress
                .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id && p.LessonId == lessonId);

            if (lessonProgress == null)
            {
                lessonProgress = new UserLessonProgress
                {
                    UserId = CurrentUser.Id,
                    LessonId = lessonId,
                    Status = ProgressStatus.NotStarted
                };
                _context.UserLessonProgress.Add(lessonProgress);
            }

            // Mark as completed if not already
            if (lessonProgress.Status != ProgressStatus.Completed)
            {
                lessonProgress.Status = ProgressStatus.Completed;
                lessonProgress.CompletedAt = DateTime.UtcNow;
                
                if (lessonProgress.StartedAt == null)
                {
                    lessonProgress.StartedAt = DateTime.UtcNow;
                }
            }

            lessonProgress.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Update module progress
            await UpdateModuleProgressAsync(moduleId);

            TempData["SuccessMessage"] = "Lesson marked as complete!";
            
            // Return the next lesson index
            await LoadLessonsAsync();
            var nextLessonIndex = GetNextIncompleteLesson();
            
            return RedirectToPage(new { id = moduleId, lessonIndex = nextLessonIndex });
        }

        private async Task<bool> CheckModuleAccessAsync(int moduleId)
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

        private async Task LoadLessonsAsync()
        {
            var orderedLessons = Module.Lessons.OrderBy(l => l.Order).ToList();
            
            Lessons = new List<LessonInfo>();
            
            for (int i = 0; i < orderedLessons.Count; i++)
            {
                var lesson = orderedLessons[i];
                
                var progress = await _context.UserLessonProgress
                    .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id && p.LessonId == lesson.Id);

                // Sequential unlocking: first lesson always unlocked, others unlock after previous is completed
                var isUnlocked = i == 0 || (i > 0 && Lessons[i - 1].Progress?.Status == ProgressStatus.Completed);

                Lessons.Add(new LessonInfo
                {
                    Lesson = lesson,
                    Progress = progress,
                    IsUnlocked = isUnlocked,
                    Quizzes = lesson.Quizzes.OrderBy(q => q.Id).ToList()
                });
            }
        }

        private int GetNextIncompleteLesson()
        {
            for (int i = 0; i < Lessons.Count; i++)
            {
                if (Lessons[i].Progress?.Status != ProgressStatus.Completed && Lessons[i].IsUnlocked)
                {
                    return i;
                }
            }
            return Math.Max(0, Lessons.Count - 1); // Return last lesson if all completed
        }

        private async Task UpdateModuleProgressAsync(int moduleId)
        {
            var moduleProgress = await _context.UserModuleProgress
                .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id && p.ModuleId == moduleId);

            if (moduleProgress != null)
            {
                var totalLessons = await _context.Lessons
                    .Where(l => l.ModuleId == moduleId && l.IsActive)
                    .CountAsync();

                var completedLessons = await _context.UserLessonProgress
                    .Where(p => p.UserId == CurrentUser.Id && 
                               p.Lesson.ModuleId == moduleId && 
                               p.Status == ProgressStatus.Completed)
                    .CountAsync();

                moduleProgress.CompletedLessons = completedLessons;
                moduleProgress.TotalLessons = totalLessons;

                // Mark module as completed if all lessons are done
                if (completedLessons >= totalLessons && totalLessons > 0)
                {
                    moduleProgress.Status = ProgressStatus.Completed;
                    moduleProgress.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
