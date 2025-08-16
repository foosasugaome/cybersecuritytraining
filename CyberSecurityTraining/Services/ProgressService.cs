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
        Task FixCompletedModuleCertificatesAsync();
        Task<bool> AreAllAssignedModulesCompletedAsync(string userId);
        Task<IEnumerable<Module>> GetAllAssignedModulesAsync(string userId);
        Task<bool> HasComprehensiveCertificateAsync(string userId);
        Task CheckAndIssueComprehensiveCertificateAsync(string userId);
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
                    moduleProgress.CertificateIssued = true;
                    moduleProgress.CertificateIssuedAt = DateTime.UtcNow;
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

            // Check if all assigned modules are completed for comprehensive certificate
            if (newStatus == ProgressStatus.Completed)
            {
                await CheckAndIssueComprehensiveCertificateAsync(userId);
            }
        }

        public async Task CheckAndIssueComprehensiveCertificateAsync(string userId)
        {
            _logger.LogInformation($"CheckAndIssueComprehensiveCertificateAsync called for user {userId}");
            
            // Check if user already has a comprehensive certificate
            var existingCertificate = await _context.UserComprehensiveCertificates
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (existingCertificate != null)
            {
                _logger.LogInformation($"User {userId} already has a comprehensive certificate issued at {existingCertificate.IssuedAt}");
                return; // Certificate already issued
            }

            // Check if all assigned modules are completed
            var allCompleted = await AreAllAssignedModulesCompletedAsync(userId);
            _logger.LogInformation($"AreAllAssignedModulesCompletedAsync returned {allCompleted} for user {userId}");
            
            if (allCompleted)
            {
                _logger.LogInformation($"Issuing comprehensive certificate for user {userId}");
                
                var completedModules = await GetAllAssignedModulesAsync(userId);
                var completedModuleIds = completedModules.Select(m => m.Id).ToList();

                var comprehensiveCertificate = new UserComprehensiveCertificate
                {
                    UserId = userId,
                    IssuedAt = DateTime.UtcNow,
                    CompletedModuleIds = System.Text.Json.JsonSerializer.Serialize(completedModuleIds),
                    TotalModulesCompleted = completedModuleIds.Count,
                    DateCreated = DateTime.UtcNow
                };

                _context.UserComprehensiveCertificates.Add(comprehensiveCertificate);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Comprehensive certificate successfully created for user {userId} with {completedModuleIds.Count} modules");
            }
            else
            {
                _logger.LogInformation($"Not all modules completed for user {userId} - certificate not issued");
            }
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

        public async Task FixCompletedModuleCertificatesAsync()
        {
            var completedModulesWithoutCertificates = await _context.UserModuleProgress
                .Where(p => p.Status == ProgressStatus.Completed && !p.CertificateIssued)
                .ToListAsync();

            foreach (var progress in completedModulesWithoutCertificates)
            {
                progress.CertificateIssued = true;
                progress.CertificateIssuedAt = progress.CompletedAt ?? DateTime.UtcNow;
            }

            if (completedModulesWithoutCertificates.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Fixed {completedModulesWithoutCertificates.Count} completed modules without certificate flags");
            }
        }

        public async Task<IEnumerable<Module>> GetAllAssignedModulesAsync(string userId)
        {
            // Get direct assignments
            var directAssignments = await _context.UserModuleAssignments
                .Where(a => a.UserId == userId)
                .Select(a => a.ModuleId)
                .ToListAsync();

            // Get group assignments from all user's active groups
            var userGroupIds = await _context.UserGroupMemberships
                .Where(m => m.UserId == userId && m.IsActive)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var groupAssignments = await _context.GroupModuleAssignments
                .Where(a => userGroupIds.Contains(a.UserGroupId))
                .Select(a => a.ModuleId)
                .ToListAsync();

            var allAssignedModuleIds = directAssignments.Union(groupAssignments).Distinct().ToList();

            return await _context.Modules
                .Where(m => allAssignedModuleIds.Contains(m.Id) && m.IsActive)
                .OrderBy(m => m.Order)
                .ToListAsync();
        }

        public async Task<bool> AreAllAssignedModulesCompletedAsync(string userId)
        {
            var assignedModules = await GetAllAssignedModulesAsync(userId);
            
            _logger.LogInformation($"AreAllAssignedModulesCompletedAsync for user {userId}: Found {assignedModules.Count()} assigned modules");
            
            if (!assignedModules.Any())
            {
                _logger.LogInformation($"User {userId} has no assigned modules");
                return false;
            }

            var completedCount = 0;
            foreach (var module in assignedModules)
            {
                var progress = await _context.UserModuleProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == module.Id);

                _logger.LogInformation($"Module {module.Id} ({module.Title}): Progress status = {(progress?.Status.ToString() ?? "No Progress")}");
                
                if (progress?.Status == ProgressStatus.Completed)
                {
                    completedCount++;
                }
                else
                {
                    _logger.LogInformation($"Module {module.Id} is not completed - returning false");
                    return false;
                }
            }

            _logger.LogInformation($"All {completedCount} assigned modules are completed for user {userId}");
            return true;
        }

        public async Task<bool> HasComprehensiveCertificateAsync(string userId)
        {
            return await _context.UserComprehensiveCertificates
                .AnyAsync(c => c.UserId == userId);
        }
    }
}
