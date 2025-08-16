using System.ComponentModel.DataAnnotations;

namespace CyberSecurityTraining.Models
{
    public class UserGroupMembership
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int UserGroupId { get; set; }
        public UserGroup UserGroup { get; set; } = null!;
        
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;
        public string? JoinedBy { get; set; } // User ID who added this user to the group
        public bool IsActive { get; set; } = true;
    }
}
