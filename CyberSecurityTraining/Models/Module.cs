using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class Module
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public int Order { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? DateModified { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<UserModuleProgress> UserProgress { get; set; } = new List<UserModuleProgress>();
        public ICollection<UserModuleAssignment> UserAssignments { get; set; } = new List<UserModuleAssignment>();
        public ICollection<GroupModuleAssignment> GroupAssignments { get; set; } = new List<GroupModuleAssignment>();
    }
}
