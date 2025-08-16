using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        
        public int PassingScore { get; set; } = 70; // Percentage
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? DateModified { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<UserQuizResult> UserResults { get; set; } = new List<UserQuizResult>();
    }
}
