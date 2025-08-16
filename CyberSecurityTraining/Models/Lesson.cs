using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty; // Markdown content
        
        public int Order { get; set; }
        
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? DateModified { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<UserLessonProgress> UserProgress { get; set; } = new List<UserLessonProgress>();
    }
}
