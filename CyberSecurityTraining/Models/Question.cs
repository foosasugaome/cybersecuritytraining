using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;
        
        public int Order { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public ICollection<UserQuestionAnswer> UserAnswers { get; set; } = new List<UserQuestionAnswer>();
    }
}
