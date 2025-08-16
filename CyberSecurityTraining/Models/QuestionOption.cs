using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class QuestionOption
    {
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public bool IsCorrect { get; set; }
        
        public int QuestionId { get; set; }
        public Question Question { get; set; } = null!;
        
        public int Order { get; set; }
        
        // Navigation properties
        public ICollection<UserQuestionAnswer> UserAnswers { get; set; } = new List<UserQuestionAnswer>();
    }
}
