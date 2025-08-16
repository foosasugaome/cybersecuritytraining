namespace CyberSecurityTraining.Models
{
    public enum ProgressStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2
    }
    
    public class UserModuleProgress
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        
        public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        
        public int CompletedLessons { get; set; } = 0;
        public int TotalLessons { get; set; } = 0;
        
        // Certificate
        public bool CertificateIssued { get; set; } = false;
        public DateTime? CertificateIssuedAt { get; set; }
    }
}
