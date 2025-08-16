namespace CyberSecurityTraining.Models
{
    public class UserQuizResult
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;
        
        public int Score { get; set; } // Percentage
        public bool Passed { get; set; }
        
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<UserQuestionAnswer> Answers { get; set; } = new List<UserQuestionAnswer>();
    }
}
