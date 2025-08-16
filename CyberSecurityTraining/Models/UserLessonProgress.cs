namespace CyberSecurityTraining.Models
{
    public class UserLessonProgress
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        
        public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        
        // For resuming where left off
        public int ScrollPosition { get; set; } = 0;
    }
}
