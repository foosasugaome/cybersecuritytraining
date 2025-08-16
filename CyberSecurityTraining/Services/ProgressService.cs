using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Services
{
    public interface IProgressService
    {
        Task<UserModuleProgress> GetOrCreateModuleProgressAsync(string userId, int moduleId);
        Task<UserLessonProgress> GetOrCreateLessonProgressAsync(string userId, int lessonId);
        Task UpdateLessonProgressAsync(string userId, int lessonId, ProgressStatus status, int scrollPosition = 0);
        Task UpdateModuleProgressAsync(string userId, int moduleId);
        Task<bool> IsModuleCompleteAsync(string userId, int moduleId);
        Task<IEnumerable<Module>> GetCompletedModulesAsync(string userId);
        Task<double> GetModuleCompletionPercentageAsync(string userId, int moduleId);
    }

    public class ProgressService : IProgressService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProgressService> _logger;

        public ProgressService(ApplicationDbContext context, ILogger<ProgressService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserModuleProgress> GetOrCreateModuleProgressAsync(string userId, int moduleId)
        {
            var progress = await _context.UserModuleProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == moduleId);

            if (progress == null)
            {
                var module = await _context.Modules
                    .Include(m => m.Lessons)
                    .FirstOrDefaultAsync(m => m.Id == moduleId);

                if (module == null)
                    throw new ArgumentException("Module not found", nameof(moduleId));

                progress = new UserModuleProgress
                {
                    UserId = userId,
                    ModuleId = moduleId,
                    Status = ProgressStatus.NotStarted,
                    TotalLessons = module.Lessons.Count,
                    CompletedLessons = 0
                };

                _context.UserModuleProgress.Add(progress);
                await _context.SaveChangesAsync();
            }

            return progress;
        }

        public async Task<UserLessonProgress> GetOrCreateLessonProgressAsync(string userId, int lessonId)
        {
            var progress = await _context.UserLessonProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    Status = ProgressStatus.NotStarted
                };

                _context.UserLessonProgress.Add(progress);
                await _context.SaveChangesAsync();
            }

            return progress;
        }

        public async Task UpdateLessonProgressAsync(string userId, int lessonId, ProgressStatus status, int scrollPosition = 0)
        {
            var progress = await GetOrCreateLessonProgressAsync(userId, lessonId);

            var wasNotStarted = progress.Status == ProgressStatus.NotStarted;
            var wasNotCompleted = progress.Status != ProgressStatus.Completed;

            progress.Status = status;
            progress.ScrollPosition = scrollPosition;
            progress.LastAccessedAt = DateTime.UtcNow;

            if (wasNotStarted && status != ProgressStatus.NotStarted)
            {
                progress.StartedAt = DateTime.UtcNow;
            }

            if (wasNotCompleted && status == ProgressStatus.Completed)
            {
                progress.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Update module progress
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson != null)
            {
                await UpdateModuleProgressAsync(userId, lesson.ModuleId);
            }
        }

        public async Task UpdateModuleProgressAsync(string userId, int moduleId)
        {
            var moduleProgress = await GetOrCreateModuleProgressAsync(userId, moduleId);

            var completedLessonsCount = await _context.UserLessonProgress
                .Join(_context.Lessons, 
                    ulp => ulp.LessonId, 
                    l => l.Id, 
                    (ulp, l) => new { ulp, l })
                .Where(x => x.ulp.UserId == userId && 
                           x.l.ModuleId == moduleId && 
                           x.ulp.Status == ProgressStatus.Completed)
                .CountAsync();

            var hasStartedAnyLesson = await _context.UserLessonProgress
                .Join(_context.Lessons,
                    ulp => ulp.LessonId,
                    l => l.Id,
                    (ulp, l) => new { ulp, l })
                .AnyAsync(x => x.ulp.UserId == userId &&
                              x.l.ModuleId == moduleId &&
                              x.ulp.Status != ProgressStatus.NotStarted);

            moduleProgress.CompletedLessons = completedLessonsCount;

            var newStatus = ProgressStatus.NotStarted;
            if (completedLessonsCount == moduleProgress.TotalLessons && moduleProgress.TotalLessons > 0)
            {
                newStatus = ProgressStatus.Completed;
                if (moduleProgress.Status != ProgressStatus.Completed)
                {
                    moduleProgress.CompletedAt = DateTime.UtcNow;
                }
            }
            else if (hasStartedAnyLesson)
            {
                newStatus = ProgressStatus.InProgress;
                if (moduleProgress.Status == ProgressStatus.NotStarted)
                {
                    moduleProgress.StartedAt = DateTime.UtcNow;
                }
            }

            moduleProgress.Status = newStatus;
            moduleProgress.LastAccessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsModuleCompleteAsync(string userId, int moduleId)
        {
            var progress = await _context.UserModuleProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == moduleId);

            return progress?.Status == ProgressStatus.Completed;
        }

        public async Task<IEnumerable<Module>> GetCompletedModulesAsync(string userId)
        {
            return await _context.UserModuleProgress
                .Where(p => p.UserId == userId && p.Status == ProgressStatus.Completed)
                .Include(p => p.Module)
                .Select(p => p.Module)
                .ToListAsync();
        }

        public async Task<double> GetModuleCompletionPercentageAsync(string userId, int moduleId)
        {
            var progress = await GetOrCreateModuleProgressAsync(userId, moduleId);

            if (progress.TotalLessons == 0)
                return 0;

            return (double)progress.CompletedLessons / progress.TotalLessons * 100;
        }
    }
}
