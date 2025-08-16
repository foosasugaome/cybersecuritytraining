using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class UserComprehensiveCertificate
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public DateTime IssuedAt { get; set; }
        public DateTime? DownloadedAt { get; set; }
        public int DownloadCount { get; set; } = 0;
        
        // Track which modules were completed for this certificate
        public string CompletedModuleIds { get; set; } = string.Empty; // JSON array of module IDs
        public int TotalModulesCompleted { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}
