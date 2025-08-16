using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class UserGroup
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        
        // Navigation properties for many-to-many relationship with users
        public ICollection<UserGroupMembership> UserMemberships { get; set; } = new List<UserGroupMembership>();
        public ICollection<GroupModuleAssignment> ModuleAssignments { get; set; } = new List<GroupModuleAssignment>();
    }
}
