namespace CyberSecurityTraining.Models
{
    public class UserQuestionAnswer
    {
        public int Id { get; set; }
        
        public int UserQuizResultId { get; set; }
        public UserQuizResult UserQuizResult { get; set; } = null!;
        
        public int QuestionId { get; set; }
        public Question Question { get; set; } = null!;
        
        public int? SelectedOptionId { get; set; }
        public QuestionOption? SelectedOption { get; set; }
        
        public bool IsCorrect { get; set; }
    }
}
