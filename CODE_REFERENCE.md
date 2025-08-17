# CyberSecurity Training Platform - Code Reference

**Version**: Current State as of August 17, 2025  
**Framework**: ASP.NET Core 9.0  
**Database**: SQLite with Entity Framework Core  
**Authentication**: ASP.NET Core Identity  
**Design System**: Ultra-Minimalist CSS Framework  

---

## 📁 Project Structure

```
CyberSecurityTraining/
├── CyberSecurityTraining.sln           # Solution file
├── docker-compose.yml                  # Docker configuration
├── README.md                          # Project documentation
├── TODO.md                            # Feature roadmap
├── PROGRESS_LOG.md                    # Development history
├── CODE_REFERENCE.md                  # This reference file
└── CyberSecurityTraining/             # Main application
    ├── Program.cs                     # Application entry point
    ├── appsettings.json              # Configuration
    ├── appsettings.Development.json  # Dev configuration
    ├── CyberSecurityTraining.csproj  # Project file
    ├── Areas/                        # Admin area
    │   └── Admin/Pages/              # Admin Razor Pages
    ├── Data/                         # Database context & migrations
    ├── Models/                       # Entity models
    ├── Pages/                        # Public Razor Pages
    ├── Services/                     # Business logic services
    └── wwwroot/                      # Static files
```

---

## 🗄️ Database Schema

### Core Entities

#### **ApplicationUser** (Identity User Extension)
```csharp
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsFirstLogin { get; set; } = true;
    public bool IsProfileComplete { get; set; } = false;
    
    // Navigation properties for all user relationships
}
```

#### **Company**
```csharp
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    
    // Navigation: Users, UserGroups
}
```

#### **Module**
```csharp
public class Module
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
    
    // Navigation: Lessons, UserModuleProgress, UserModuleAssignments, GroupModuleAssignments
}
```

#### **Lesson**
```csharp
public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // Markdown content
    public int Order { get; set; }
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
    
    // Navigation: Quizzes, UserLessonProgress
}
```

#### **Quiz**
```csharp
public class Quiz
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PassingScore { get; set; } = 70;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
    
    // Navigation: Questions, UserQuizResults
}
```

#### **Question**
```csharp
public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    
    // Navigation: Options, UserQuestionAnswers
}
```

#### **QuestionOption**
```csharp
public class QuestionOption
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    
    // Navigation: UserQuestionAnswers
}
```

### Progress Tracking Entities

#### **UserModuleProgress**
```csharp
public class UserModuleProgress
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public bool CertificateIssued { get; set; } = false;
    public DateTime? CertificateIssuedAt { get; set; }
}
```

#### **UserLessonProgress**
```csharp
public class UserLessonProgress
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}
```

#### **UserQuizResult**
```csharp
public class UserQuizResult
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int Score { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
    
    // Navigation: Answers
}
```

### Assignment & Grouping Entities

#### **UserGroup**
```csharp
public class UserGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public DateTime DateCreated { get; set; }
    
    // Navigation: Memberships, ModuleAssignments
}
```

#### **UserGroupMembership**
```csharp
public class UserGroupMembership
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime DateJoined { get; set; }
}
```

#### **UserModuleAssignment**
```csharp
public class UserModuleAssignment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }
}
```

#### **GroupModuleAssignment**
```csharp
public class GroupModuleAssignment
{
    public int Id { get; set; }
    public int UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }
}
```

### Certificate Entities

#### **UserComprehensiveCertificate**
```csharp
public class UserComprehensiveCertificate
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string ModuleIds { get; set; } = string.Empty; // JSON array
    public DateTime IssuedAt { get; set; }
    public DateTime? DownloadedAt { get; set; }
    public int DownloadCount { get; set; } = 0;
}
```

---

## 🔧 Core Services

### **ICertificateService**
```csharp
public interface ICertificateService
{
    Task<byte[]> GenerateModuleCertificateAsync(ApplicationUser user, Module module);
    Task<byte[]> GenerateCompletionCertificateAsync(ApplicationUser user, List<Module> modules);
}
```

### **IProgressService**
```csharp
public interface IProgressService
{
    Task<UserModuleProgress> UpdateModuleProgressAsync(string userId, int moduleId);
    Task<UserLessonProgress> UpdateLessonProgressAsync(string userId, int lessonId);
    Task FixCompletedModuleCertificatesAsync();
    Task<bool> IsModuleCompletedAsync(string userId, int moduleId);
    Task<double> GetModuleProgressPercentageAsync(string userId, int moduleId);
}
```

### **IEmailService**
```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendWelcomeEmailAsync(ApplicationUser user, string password);
}
```

### **IMarkdownService**
```csharp
public interface IMarkdownService
{
    string ConvertToHtml(string markdown);
}
```

---

## 🎯 Key Enums

### **ProgressStatus**
```csharp
public enum ProgressStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}
```

---

## 🔐 Authentication & Authorization

### **Roles**
- **Admin**: Full system access, can manage all entities
- **User**: Can access assigned training modules

### **Policies**
- **AdminOnly**: Requires Admin role
- **UserOnly**: Requires User role

### **Protected Areas**
- `/Admin/*` - Admin area (AdminOnly policy)
- `/Training/*` - Training area (Authenticated users)

---

## 🚀 Application Entry Point (Program.cs)

### **Key Configuration**
```csharp
// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IMarkdownService, MarkdownService>();
```

### **Authorization Policies**
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// Page authorization
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminOnly");
    options.Conventions.AuthorizePage("/Training/Dashboard");
    options.Conventions.AuthorizePage("/Training/Module");
    options.Conventions.AuthorizePage("/Training/Lesson");
    options.Conventions.AuthorizePage("/Training/Quiz");
});
```

---

## 📱 Page Structure

### **Public Pages** (`/Pages/`)
- `/Index` - Landing page
- `/Privacy` - Privacy policy
- `/Error` - Error handling
- `/Error404` - Custom 404 page
- `/Account/` - Authentication pages
- `/Training/` - Training modules for authenticated users

### **Admin Pages** (`/Areas/Admin/Pages/`)
- `/Dashboard` - Admin dashboard
- `/Companies/` - Company management
- `/Users/` - User management
- `/Modules/` - Module management
- `/Lessons/` - Lesson management
- `/Questions/` - Question management
- `/UserGroups/` - User group management

---

## 🗂️ Database Context (ApplicationDbContext)

### **Key DbSets**
```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserGroupMembership> UserGroupMemberships { get; set; }
    public DbSet<UserModuleAssignment> UserModuleAssignments { get; set; }
    public DbSet<GroupModuleAssignment> GroupModuleAssignments { get; set; }
    public DbSet<UserModuleProgress> UserModuleProgress { get; set; }
    public DbSet<UserLessonProgress> UserLessonProgress { get; set; }
    public DbSet<UserQuizResult> UserQuizResults { get; set; }
    public DbSet<UserQuestionAnswer> UserQuestionAnswers { get; set; }
    public DbSet<UserComprehensiveCertificate> UserComprehensiveCertificates { get; set; }
}
```

---

## 🌱 Seeding Data (SeedData.cs)

### **Default Admin User**
- **Email**: `admin@cybersec.local`
- **Password**: `Admin123!`
- **Role**: Admin

### **Sample Data Includes**
- Companies (TechCorp, SecureFinance, EduTech)
- Training modules (Cybersecurity Fundamentals, Advanced Threats, etc.)
- Lessons with markdown content
- Quizzes with multiple-choice questions
- User groups and assignments

---

## 🔄 Common Patterns

### **Null-Safe Property Assignment**
```csharp
// Correct pattern for nullable references
var entity = await context.Entities.FirstOrDefaultAsync(e => e.Id == id);
if (entity == null)
{
    return NotFound();
}
Property = entity;
```

### **User Authentication Check**
```csharp
var currentUser = await _userManager.GetUserAsync(User);
if (currentUser == null)
{
    return Challenge();
}
CurrentUser = currentUser;
```

### **Progress Tracking Update**
```csharp
await _progressService.UpdateLessonProgressAsync(CurrentUser.Id, lessonId);
await _progressService.UpdateModuleProgressAsync(CurrentUser.Id, moduleId);
```

---

## 🛠️ Development Guidelines

### **Code Quality Standards**
- ✅ Zero build warnings
- ✅ Proper null reference handling
- ✅ Async/await best practices
- ✅ Entity Framework best practices
- ✅ Proper error handling with try-catch

### **Database Best Practices**
- Always include null checks for `FirstOrDefaultAsync()` results
- Use proper navigation properties for relationships
- Include related data with `.Include()` when needed
- Use transactions for complex operations

### **Security Considerations**
- All admin pages protected with `AdminOnly` policy
- Training pages require authentication
- User data access properly scoped by user ID
- Input validation on all forms

---

## 📦 Dependencies

### **Key NuGet Packages**
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Tools`
- `Markdig` (Markdown processing)

### **Static Assets**
- Ultra-Minimalist CSS Framework (custom)
- Font Awesome 6.4.0
- jQuery 3.7.0
- Custom CSS in `/wwwroot/css/`

---

## 🏗️ Build & Deployment

### **Development Commands**
```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run development server
dotnet run --environment Development

# Database migrations
dotnet ef migrations add MigrationName
dotnet ef database update

# Run tests
dotnet test
```

### **Database Connection**
- **Development**: SQLite file `cybersecurity_training.db`
- **Connection String**: `Data Source=cybersecurity_training.db`

---

## 📋 Current Status

### **Features Implemented** ✅
- User authentication and authorization
- Company and user management
- Training module creation and management
- Lesson content with Markdown support
- Quiz system with multiple-choice questions
- Progress tracking for modules and lessons
- Certificate generation (PDF)
- User group management and assignments
- Admin dashboard with analytics
- Ultra-minimalist responsive design system
- Consistent table styling across admin pages
- Modern information grid layouts

### **Database State** ✅
- Fresh database with comprehensive seed data
- All migrations applied
- Sample content ready for testing
- Admin user configured

### **Code Quality** ✅
- Zero build warnings
- Proper error handling
- Null-safe coding patterns
- Clean architecture with service layer

---

## 🔍 Quick Reference Commands

### **Find Entity by ID Pattern**
```csharp
var entity = await _context.Entities
    .Include(e => e.RelatedEntity)
    .FirstOrDefaultAsync(e => e.Id == id);

if (entity == null)
{
    return NotFound();
}
```

### **Check User Access to Module**
```csharp
private async Task<bool> CheckModuleAccessAsync(int moduleId)
{
    // Check direct assignment
    var directAssignment = await _context.UserModuleAssignments
        .AnyAsync(a => a.UserId == CurrentUser.Id && a.ModuleId == moduleId);

    if (directAssignment) return true;

    // Check group assignment
    var userGroupIds = await _context.UserGroupMemberships
        .Where(m => m.UserId == CurrentUser.Id && m.IsActive)
        .Select(m => m.UserGroupId)
        .ToListAsync();

    var groupAssignment = await _context.GroupModuleAssignments
        .AnyAsync(a => userGroupIds.Contains(a.UserGroupId) && a.ModuleId == moduleId);

    return groupAssignment;
}
```

---

*This reference file should be consulted before making any changes to ensure consistency with the current architecture and patterns.*
