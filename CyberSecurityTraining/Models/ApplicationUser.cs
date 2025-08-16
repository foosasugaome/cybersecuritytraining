using Microsoft.AspNetCore.Identity;

namespace CyberSecurityTraining.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsFirstLogin { get; set; } = true;
        public bool IsProfileComplete { get; set; } = false;
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        
        // Navigation properties for many-to-many relationship with groups
        public ICollection<UserGroupMembership> GroupMemberships { get; set; } = new List<UserGroupMembership>();
        public ICollection<UserModuleProgress> ModuleProgress { get; set; } = new List<UserModuleProgress>();
        public ICollection<UserQuizResult> QuizResults { get; set; } = new List<UserQuizResult>();
        public ICollection<UserModuleAssignment> ModuleAssignments { get; set; } = new List<UserModuleAssignment>();
    }
}
