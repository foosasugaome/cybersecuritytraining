using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class Company
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}
