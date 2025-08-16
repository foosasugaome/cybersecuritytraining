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
    public class CertificateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICertificateService _certificateService;
        private readonly IProgressService _progressService;

        public CertificateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICertificateService certificateService,
            IProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _certificateService = certificateService;
            _progressService = progressService;
        }

        public Module Module { get; set; } = default!;
        public UserModuleProgress ModuleProgress { get; set; } = default!;
        public ApplicationUser CurrentUser { get; set; } = default!;
        public UserComprehensiveCertificate? ComprehensiveCertificate { get; set; }
        public List<Module> CompletedModules { get; set; } = new();
        public bool IsComprehensiveCertificate { get; set; }

        public async Task<IActionResult> OnGetAsync(int? moduleId)
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return Challenge();
            }

            // Check if this is a comprehensive certificate request (no moduleId)
            if (moduleId == null)
            {
                return await HandleComprehensiveCertificateAsync();
            }

            // Handle individual module certificate (legacy support)
            return await HandleModuleCertificateAsync(moduleId.Value);
        }

        private async Task<IActionResult> HandleComprehensiveCertificateAsync()
        {
            IsComprehensiveCertificate = true;

            // Get comprehensive certificate
            ComprehensiveCertificate = await _context.UserComprehensiveCertificates
                .FirstOrDefaultAsync(c => c.UserId == CurrentUser!.Id);

            if (ComprehensiveCertificate == null)
            {
                TempData["ErrorMessage"] = "Comprehensive certificate is not yet available. Complete all assigned modules first.";
                return RedirectToPage("/Training/Dashboard");
            }

            // Get completed modules for display
            var moduleIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(ComprehensiveCertificate.CompletedModuleIds);
            CompletedModules = await _context.Modules
                .Where(m => moduleIds!.Contains(m.Id))
                .OrderBy(m => m.Order)
                .ToListAsync();

            return Page();
        }

        private async Task<IActionResult> HandleModuleCertificateAsync(int moduleId)
        {
            IsComprehensiveCertificate = false;

            Module = await _context.Modules
                .Where(m => m.Id == moduleId && m.IsActive)
                .FirstOrDefaultAsync();

            if (Module == null)
            {
                return NotFound();
            }

            // Check if user has access to this module
            var hasAccess = await CheckModuleAccessAsync(Module.Id);
            if (!hasAccess)
            {
                return Forbid();
            }

            // Get module progress
            ModuleProgress = await _context.UserModuleProgress
                .Where(p => p.UserId == CurrentUser!.Id && p.ModuleId == Module.Id)
                .FirstOrDefaultAsync();

            if (ModuleProgress == null || ModuleProgress.Status != ProgressStatus.Completed || !ModuleProgress.CertificateIssued)
            {
                TempData["ErrorMessage"] = "Certificate is only available for completed modules.";
                return RedirectToPage("/Training/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadAsync(int? moduleId)
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return Challenge();
            }

            // Check if this is a comprehensive certificate download
            if (moduleId == null)
            {
                return await DownloadComprehensiveCertificateAsync();
            }

            // Handle individual module certificate download (legacy support)
            return await DownloadModuleCertificateAsync(moduleId.Value);
        }

        private async Task<IActionResult> DownloadComprehensiveCertificateAsync()
        {
            var comprehensiveCertificate = await _context.UserComprehensiveCertificates
                .FirstOrDefaultAsync(c => c.UserId == CurrentUser!.Id);

            if (comprehensiveCertificate == null)
            {
                TempData["ErrorMessage"] = "Comprehensive certificate is not available.";
                return RedirectToPage();
            }

            try
            {
                // Get completed modules for certificate generation
                var moduleIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(comprehensiveCertificate.CompletedModuleIds);
                var completedModules = await _context.Modules
                    .Where(m => moduleIds!.Contains(m.Id))
                    .OrderBy(m => m.Order)
                    .ToListAsync();

                var certificateBytes = await _certificateService.GenerateCompletionCertificateAsync(CurrentUser, completedModules);
                var fileName = $"CyberSecurity_Certificate_{CurrentUser.FirstName}_{CurrentUser.LastName}.pdf";

                // Update download tracking
                comprehensiveCertificate.DownloadedAt = DateTime.UtcNow;
                comprehensiveCertificate.DownloadCount++;
                await _context.SaveChangesAsync();

                return File(certificateBytes, "application/pdf", fileName);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while generating the certificate. Please try again.";
                return RedirectToPage();
            }
        }

        private async Task<IActionResult> DownloadModuleCertificateAsync(int moduleId)
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return Challenge();
            }

            Module = await _context.Modules
                .Where(m => m.Id == moduleId && m.IsActive)
                .FirstOrDefaultAsync();

            if (Module == null)
            {
                return NotFound();
            }

            // Check if user has access to this module
            var hasAccess = await CheckModuleAccessAsync(Module.Id);
            if (!hasAccess)
            {
                return Forbid();
            }

            // Get module progress
            ModuleProgress = await _context.UserModuleProgress
                .Where(p => p.UserId == CurrentUser.Id && p.ModuleId == Module.Id)
                .FirstOrDefaultAsync();

            if (ModuleProgress == null || ModuleProgress.Status != ProgressStatus.Completed || !ModuleProgress.CertificateIssued)
            {
                TempData["ErrorMessage"] = "Certificate is only available for completed modules.";
                return RedirectToPage("/Training/Dashboard");
            }

            try
            {
                var certificateBytes = await _certificateService.GenerateCertificateAsync(CurrentUser, Module);
                var fileName = $"Certificate_{Module.Title.Replace(" ", "_")}_{CurrentUser.FirstName}_{CurrentUser.LastName}.pdf";
                
                return File(certificateBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while generating the certificate. Please try again.";
                return RedirectToPage();
            }
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
    }
}
