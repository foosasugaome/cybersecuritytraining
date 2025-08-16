using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserGroupMembership> UserGroupMemberships { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<UserModuleProgress> UserModuleProgress { get; set; }
        public DbSet<UserLessonProgress> UserLessonProgress { get; set; }
        public DbSet<UserQuizResult> UserQuizResults { get; set; }
        public DbSet<UserQuestionAnswer> UserQuestionAnswers { get; set; }
        public DbSet<UserModuleAssignment> UserModuleAssignments { get; set; }
        public DbSet<GroupModuleAssignment> GroupModuleAssignments { get; set; }
        public DbSet<UserComprehensiveCertificate> UserComprehensiveCertificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            
            // Company relationships
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserGroup>()
                .HasOne(g => g.Company)
                .WithMany(c => c.UserGroups)
                .HasForeignKey(g => g.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserGroup relationships - Many-to-Many through UserGroupMembership
            modelBuilder.Entity<UserGroupMembership>()
                .HasOne(m => m.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserGroupMembership>()
                .HasOne(m => m.UserGroup)
                .WithMany(g => g.UserMemberships)
                .HasForeignKey(m => m.UserGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Module relationships
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Module)
                .WithMany(m => m.Lessons)
                .HasForeignKey(l => l.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quiz relationships
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lesson)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question relationships
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionOption>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Progress relationships
            modelBuilder.Entity<UserModuleProgress>()
                .HasOne(p => p.User)
                .WithMany(u => u.ModuleProgress)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModuleProgress>()
                .HasOne(p => p.Module)
                .WithMany(m => m.UserProgress)
                .HasForeignKey(p => p.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLessonProgress>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLessonProgress>()
                .HasOne(p => p.Lesson)
                .WithMany(l => l.UserProgress)
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quiz result relationships
            modelBuilder.Entity<UserQuizResult>()
                .HasOne(r => r.User)
                .WithMany(u => u.QuizResults)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserQuizResult>()
                .HasOne(r => r.Quiz)
                .WithMany(q => q.UserResults)
                .HasForeignKey(r => r.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quiz answer relationships
            modelBuilder.Entity<UserQuestionAnswer>()
                .HasOne(a => a.UserQuizResult)
                .WithMany(r => r.Answers)
                .HasForeignKey(a => a.UserQuizResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserQuestionAnswer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.UserAnswers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserQuestionAnswer>()
                .HasOne(a => a.SelectedOption)
                .WithMany(o => o.UserAnswers)
                .HasForeignKey(a => a.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Assignment relationships
            modelBuilder.Entity<UserModuleAssignment>()
                .HasOne(a => a.User)
                .WithMany(u => u.ModuleAssignments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModuleAssignment>()
                .HasOne(a => a.Module)
                .WithMany(m => m.UserAssignments)
                .HasForeignKey(a => a.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupModuleAssignment>()
                .HasOne(a => a.UserGroup)
                .WithMany(g => g.ModuleAssignments)
                .HasForeignKey(a => a.UserGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupModuleAssignment>()
                .HasOne(a => a.Module)
                .WithMany(m => m.GroupAssignments)
                .HasForeignKey(a => a.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraints
            modelBuilder.Entity<UserModuleProgress>()
                .HasIndex(p => new { p.UserId, p.ModuleId })
                .IsUnique();

            modelBuilder.Entity<UserLessonProgress>()
                .HasIndex(p => new { p.UserId, p.LessonId })
                .IsUnique();

            modelBuilder.Entity<UserModuleAssignment>()
                .HasIndex(a => new { a.UserId, a.ModuleId })
                .IsUnique();

            modelBuilder.Entity<GroupModuleAssignment>()
                .HasIndex(a => new { a.UserGroupId, a.ModuleId })
                .IsUnique();
        }
    }
}
