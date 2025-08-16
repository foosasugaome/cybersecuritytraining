namespace CyberSecurityTraining.Models
{
    public class UserModuleAssignment
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; } = string.Empty; // Admin user ID
        
        public DateTime? DueDate { get; set; }
    }
}
