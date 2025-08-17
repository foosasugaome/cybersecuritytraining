using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Services
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Clear existing data
            await ClearDatabaseAsync(context);

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed admin user
            await SeedAdminUserAsync(userManager);

            // Seed sample data
            await SeedSampleDataAsync(context, userManager);
        }

        private static async Task ClearDatabaseAsync(ApplicationDbContext context)
        {
            // Remove all data in correct order to avoid foreign key constraints
            context.QuestionOptions.RemoveRange(context.QuestionOptions);
            context.Questions.RemoveRange(context.Questions);
            context.Quizzes.RemoveRange(context.Quizzes);
            context.Lessons.RemoveRange(context.Lessons);
            context.Modules.RemoveRange(context.Modules);
            context.UserGroups.RemoveRange(context.UserGroups);
            context.Companies.RemoveRange(context.Companies);
            
            // Remove users (keeping admin for now)
            var usersToRemove = context.Users.Where(u => u.Email != "admin@cybersec.local");
            context.Users.RemoveRange(usersToRemove);
            
            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            if (await userManager.FindByEmailAsync("admin@cybersec.local") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@admin.local",
                    Email = "admin@admin.local",
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsFirstLogin = false,
                    IsProfileComplete = true,
                    DateCreated = DateTime.UtcNow
                };

                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        private static async Task SeedSampleDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Seed multiple companies
            var companies = new List<Company>
            {
                new Company
                {
                    Name = "TechCorp Industries",
                    Description = "A leading technology company focused on cybersecurity solutions",
                    DateCreated = DateTime.UtcNow
                },
                new Company
                {
                    Name = "SecureBank Financial",
                    Description = "A financial institution with high security requirements",
                    DateCreated = DateTime.UtcNow
                },
                new Company
                {
                    Name = "HealthTech Medical",
                    Description = "Healthcare technology company handling sensitive patient data",
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Companies.AddRange(companies);
            await context.SaveChangesAsync();

            // Seed user groups for each company
            var userGroups = new List<UserGroup>
            {
                // TechCorp groups
                new UserGroup { Name = "IT Security Team", Description = "Cybersecurity specialists and analysts", CompanyId = companies[0].Id, DateCreated = DateTime.UtcNow },
                new UserGroup { Name = "Development Team", Description = "Software developers and engineers", CompanyId = companies[0].Id, DateCreated = DateTime.UtcNow },
                new UserGroup { Name = "Management", Description = "Executive and management staff", CompanyId = companies[0].Id, DateCreated = DateTime.UtcNow },
                
                // SecureBank groups
                new UserGroup { Name = "Risk Management", Description = "Financial risk and compliance team", CompanyId = companies[1].Id, DateCreated = DateTime.UtcNow },
                new UserGroup { Name = "IT Operations", Description = "Banking IT infrastructure team", CompanyId = companies[1].Id, DateCreated = DateTime.UtcNow },
                
                // HealthTech groups
                new UserGroup { Name = "Data Security", Description = "Patient data protection specialists", CompanyId = companies[2].Id, DateCreated = DateTime.UtcNow },
                new UserGroup { Name = "Clinical IT", Description = "Healthcare IT support staff", CompanyId = companies[2].Id, DateCreated = DateTime.UtcNow }
            };

            context.UserGroups.AddRange(userGroups);
            await context.SaveChangesAsync();

            // Seed sample users
            var sampleUsers = new List<(string email, string firstName, string lastName, string password, int companyIndex, int groupIndex)>
            {
                ("alice.johnson@techcorp.com", "Alice", "Johnson", "User123!", 0, 0),
                ("bob.smith@techcorp.com", "Bob", "Smith", "User123!", 0, 1),
                ("carol.manager@techcorp.com", "Carol", "Manager", "User123!", 0, 2),
                ("david.risk@securebank.com", "David", "Risk", "User123!", 1, 3),
                ("eve.ops@securebank.com", "Eve", "Operations", "User123!", 1, 4),
                ("frank.security@healthtech.com", "Frank", "Security", "User123!", 2, 5),
                ("grace.clinical@healthtech.com", "Grace", "Clinical", "User123!", 2, 6)
            };

            foreach (var (email, firstName, lastName, password, companyIndex, groupIndex) in sampleUsers)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        EmailConfirmed = true,
                        IsFirstLogin = false,
                        IsProfileComplete = true,
                        DateCreated = DateTime.UtcNow,
                        CompanyId = companies[companyIndex].Id
                    };

                    await userManager.CreateAsync(user, password);
                    await userManager.AddToRoleAsync(user, "User");

                    // Add user to group using the new many-to-many relationship
                    var membership = new UserGroupMembership
                    {
                        UserId = user.Id,
                        UserGroupId = userGroups[groupIndex].Id,
                        DateJoined = DateTime.UtcNow,
                        JoinedBy = "System",
                        IsActive = true
                    };
                    context.UserGroupMemberships.Add(membership);
                }
            }

            // Save all user group memberships
            await context.SaveChangesAsync();

            // Seed comprehensive modules
            var modules = new List<Module>
            {
                new Module
                {
                    Title = "Cybersecurity Fundamentals",
                    Description = "Essential cybersecurity concepts, threats, and defense strategies for all employees",
                    Order = 1,
                    DateCreated = DateTime.UtcNow
                },
                new Module
                {
                    Title = "Password Security & Authentication",
                    Description = "Best practices for password creation, management, and multi-factor authentication",
                    Order = 2,
                    DateCreated = DateTime.UtcNow
                },
                new Module
                {
                    Title = "Email Security & Phishing",
                    Description = "Identifying and preventing email-based threats, phishing attacks, and social engineering",
                    Order = 3,
                    DateCreated = DateTime.UtcNow
                },
                new Module
                {
                    Title = "Network Security Basics",
                    Description = "Understanding network threats, secure connections, and safe browsing practices",
                    Order = 4,
                    DateCreated = DateTime.UtcNow
                },
                new Module
                {
                    Title = "Data Protection & Privacy",
                    Description = "Protecting sensitive data, understanding privacy regulations, and secure data handling",
                    Order = 5,
                    DateCreated = DateTime.UtcNow
                },
                new Module
                {
                    Title = "Incident Response & Recovery",
                    Description = "How to respond to security incidents, report breaches, and recover from attacks",
                    Order = 6,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Modules.AddRange(modules);
            await context.SaveChangesAsync();

            // Seed lessons for each module
            await SeedLessonsAndQuizzesAsync(context, modules);
        }

        private static async Task SeedLessonsAndQuizzesAsync(ApplicationDbContext context, List<Module> modules)
        {
            // Module 1: Cybersecurity Fundamentals
            var module1Lessons = new List<Lesson>
            {
                new Lesson
                {
                    Title = "What is Cybersecurity?",
                    Content = @"# What is Cybersecurity?

Cybersecurity is the practice of protecting critical systems and sensitive information from digital attacks. Also known as information technology (IT) security, cybersecurity measures are designed to combat threats against networked systems and applications, whether those threats originate from inside or outside of an organization.

## Why is Cybersecurity Important?

In today's connected world, everyone benefits from advanced cyberdefense programs. At an individual level, a cybersecurity attack can result in everything from identity theft, to extortion attempts, to the loss of important data like family photos. Everyone relies on critical infrastructure like power plants, hospitals, and financial service companies.

## Common Cybersecurity Threats

1. **Malware** - Software designed to damage or gain unauthorized access to computer systems
2. **Phishing** - Fraudulent attempts to obtain sensitive information  
3. **Social Engineering** - Psychological manipulation to gain access to systems
4. **Ransomware** - Malicious software that encrypts data and demands payment
5. **Data Breaches** - Unauthorized access to confidential information

## The CIA Triad

The foundation of cybersecurity is built on three key principles:
- **Confidentiality**: Ensuring information is accessible only to authorized users
- **Integrity**: Maintaining the accuracy and completeness of data
- **Availability**: Ensuring systems and data are accessible when needed",
                    Order = 1,
                    ModuleId = modules[0].Id,
                    DateCreated = DateTime.UtcNow
                },
                new Lesson
                {
                    Title = "Types of Cyber Threats",
                    Content = @"# Types of Cyber Threats

Understanding different types of cyber threats is crucial for effective defense. Threats can be categorized by their origin, method, and target.

## By Origin

### External Threats
- **Cybercriminals**: Motivated by financial gain
- **Nation-state actors**: Government-sponsored attacks
- **Hacktivists**: Ideologically motivated attackers
- **Script kiddies**: Inexperienced attackers using pre-made tools

### Internal Threats
- **Malicious insiders**: Employees with harmful intent
- **Negligent insiders**: Employees who accidentally cause breaches
- **Compromised insiders**: Employees whose accounts have been hijacked

## By Method

### Technical Attacks
- Exploiting software vulnerabilities
- Network-based attacks
- System configuration weaknesses

### Social Engineering
- Manipulating human psychology
- Building trust to gain access
- Exploiting human error and emotions

## Impact Categories

### Financial Impact
- Direct monetary losses
- Business disruption costs
- Recovery and remediation expenses
- Legal and regulatory fines

### Reputational Impact
- Loss of customer trust
- Brand damage
- Competitive disadvantage
- Long-term market impact",
                    Order = 2,
                    ModuleId = modules[0].Id,
                    DateCreated = DateTime.UtcNow
                },
                new Lesson
                {
                    Title = "Cybersecurity Best Practices",
                    Content = @"# Cybersecurity Best Practices

Implementing basic cybersecurity practices significantly reduces risk and improves overall security posture.

## Personal Security Practices

### Device Security
- Keep operating systems and software updated
- Use reputable antivirus software
- Enable automatic security updates
- Secure physical access to devices
- Use device encryption

### Online Behavior
- Be cautious with downloads and attachments
- Verify website authenticity before entering sensitive information
- Use secure connections (HTTPS) for sensitive activities
- Log out of accounts when finished
- Be mindful of public Wi-Fi risks

## Workplace Security Practices

### Access Control
- Use principle of least privilege
- Regularly review and update access permissions
- Implement strong authentication methods
- Monitor and log access activities

### Data Handling
- Classify data by sensitivity level
- Follow data retention policies
- Use approved cloud storage services
- Encrypt sensitive data in transit and at rest

### Incident Reporting
- Report suspicious activities immediately
- Follow established incident response procedures
- Document security incidents properly
- Learn from incidents to prevent recurrence

## Security Awareness

### Stay Informed
- Follow cybersecurity news and trends
- Participate in security training programs
- Understand your organization's security policies
- Know who to contact for security questions

### Risk Assessment
- Identify potential threats to your role
- Understand the value of data you handle
- Recognize your responsibilities in security
- Balance security with productivity needs",
                    Order = 3,
                    ModuleId = modules[0].Id,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Lessons.AddRange(module1Lessons);
            await context.SaveChangesAsync();

            // Module 2: Password Security & Authentication
            var module2Lessons = new List<Lesson>
            {
                new Lesson
                {
                    Title = "Password Security Fundamentals",
                    Content = @"# Password Security Fundamentals

Passwords are the most common form of authentication, making password security critical for protecting accounts and systems.

## Password Vulnerabilities

### Common Password Attacks
- **Brute Force**: Systematically trying all possible combinations
- **Dictionary Attacks**: Using common passwords and dictionary words
- **Credential Stuffing**: Using leaked passwords from other breaches
- **Password Spraying**: Trying common passwords against many accounts

### Weak Password Characteristics
- Short length (less than 12 characters)
- Dictionary words or common phrases
- Personal information (names, birthdays, addresses)
- Predictable patterns (123456, qwerty, password123)
- Reused across multiple accounts

## Strong Password Creation

### Length and Complexity
- Minimum 12 characters (longer is better)
- Mix of uppercase and lowercase letters
- Include numbers and special characters
- Avoid predictable substitutions (@ for a, 3 for e)

### Passphrase Method
- Use multiple random words
- Add numbers and symbols
- Example: Coffee#Mountain$Train9
- Easier to remember than complex character strings

### Password Generation
- Use password managers to generate random passwords
- Avoid human-created patterns
- Generate unique passwords for each account
- Store generated passwords securely

## Password Storage and Management

### Never Store Passwords In:
- Plain text files on your computer
- Browser password managers (for sensitive accounts)
- Email or messaging applications
- Physical notes in accessible locations

### Secure Storage Methods:
- Dedicated password managers
- Encrypted password databases
- Secure corporate password systems
- Hardware security keys for high-value accounts",
                    Order = 1,
                    ModuleId = modules[1].Id,
                    DateCreated = DateTime.UtcNow
                },
                new Lesson
                {
                    Title = "Multi-Factor Authentication (MFA)",
                    Content = @"# Multi-Factor Authentication (MFA)

Multi-Factor Authentication adds extra layers of security beyond just passwords, significantly improving account protection.

## Authentication Factors

### Something You Know (Knowledge)
- Passwords and passphrases
- PINs and security questions
- Patterns and codes

### Something You Have (Possession)
- Smartphones and tablets
- Hardware security keys
- Smart cards and tokens
- Backup codes

### Something You Are (Inherence)
- Fingerprints and face recognition
- Retina and iris scans
- Voice recognition
- Behavioral biometrics

## Types of MFA

### SMS-Based Authentication
- **Pros**: Widely supported, easy to use
- **Cons**: Vulnerable to SIM swapping and interception
- **Best for**: Low-risk accounts when other options unavailable

### App-Based Authentication
- **Examples**: Google Authenticator, Microsoft Authenticator, Authy
- **Pros**: More secure than SMS, works offline
- **Cons**: Can be lost with device, requires setup
- **Best for**: Most online accounts and services

### Hardware Security Keys
- **Examples**: YubiKey, Google Titan, RSA SecurID
- **Pros**: Highest security, phishing-resistant
- **Cons**: Cost, can be lost or forgotten
- **Best for**: High-value accounts, administrative access

### Push Notifications
- **How it works**: App sends approval request to registered device
- **Pros**: User-friendly, real-time verification
- **Cons**: Requires internet connection, notification fatigue
- **Best for**: Corporate environments with managed devices

## MFA Implementation Best Practices

### Account Prioritization
1. **Critical accounts**: Email, banking, work systems
2. **Important accounts**: Social media, shopping, cloud storage
3. **Convenience accounts**: Entertainment, news, low-risk services

### Recovery Planning
- Store backup codes in secure location
- Register multiple authentication methods
- Keep recovery contact information updated
- Understand account recovery processes

### Security Considerations
- Avoid using the same authentication method for backup
- Regularly review and update MFA settings
- Be aware of MFA fatigue attacks
- Report suspicious authentication requests",
                    Order = 2,
                    ModuleId = modules[1].Id,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Lessons.AddRange(module2Lessons);
            await context.SaveChangesAsync();

            // Continue with other modules...
            // I'll add a few more lessons for demonstration
            var module3Lessons = new List<Lesson>
            {
                new Lesson
                {
                    Title = "Email Security Fundamentals",
                    Content = @"# Email Security Fundamentals

Email remains one of the most common attack vectors for cybercriminals. Understanding email security is essential for protecting yourself and your organization.

## Email Threat Landscape

### Common Email Attacks
- **Phishing**: Fraudulent emails designed to steal credentials or information
- **Spear Phishing**: Targeted phishing attacks against specific individuals
- **Business Email Compromise (BEC)**: Impersonating executives or vendors
- **Malware Delivery**: Attachments or links containing malicious software
- **Spam**: Unsolicited emails that may contain threats or scams

### Email Attack Indicators
- Urgent or threatening language
- Requests for sensitive information
- Suspicious sender addresses or domains
- Generic greetings (""Dear Customer"")
- Poor grammar and spelling
- Unexpected attachments or links
- Requests to bypass security procedures

## Email Security Best Practices

### Sender Verification
- Check sender email addresses carefully
- Look for domain spoofing (similar but fake domains)
- Verify unexpected requests through alternative communication
- Be cautious of emails from unknown senders
- Pay attention to email display names vs. actual addresses

### Link and Attachment Safety
- Hover over links to preview destinations
- Type URLs manually instead of clicking
- Scan attachments with antivirus software
- Be wary of unexpected file types (.exe, .scr, .zip)
- Use cloud-based document sharing when possible

### Information Protection
- Never send passwords or sensitive data via email
- Use encrypted email for confidential communications
- Be careful about what information you share
- Consider who is included in CC and BCC fields
- Use secure file sharing services for sensitive documents",
                    Order = 1,
                    ModuleId = modules[2].Id,
                    DateCreated = DateTime.UtcNow
                },
                new Lesson
                {
                    Title = "Identifying Phishing Attacks",
                    Content = @"# Identifying Phishing Attacks

Phishing attacks are becoming increasingly sophisticated. Learning to identify these attacks is crucial for protecting yourself and your organization.

## Types of Phishing Attacks

### Email Phishing
- **Mass phishing**: Generic attacks sent to many recipients
- **Spear phishing**: Targeted attacks using personal information
- **Whaling**: Attacks targeting high-profile individuals
- **Clone phishing**: Copying legitimate emails with malicious modifications

### Non-Email Phishing
- **Smishing**: SMS/text message phishing
- **Vishing**: Voice/phone call phishing
- **Social media phishing**: Attacks through social platforms
- **Website spoofing**: Fake websites mimicking legitimate ones

## Red Flags to Watch For

### Email Content Indicators
- Urgent deadlines or threats (""Account will be closed"")
- Requests for personal or financial information
- Generic greetings instead of your name
- Poor grammar, spelling, or formatting
- Mismatched or suspicious URLs
- Unexpected attachments

### Technical Indicators
- Sender address doesn't match claimed organization
- Links redirect to suspicious domains
- Email lacks proper security headers
- Requests to download unusual software
- Asks to disable security features

## Verification Techniques

### Before Taking Action
1. **Pause and think**: Don't act immediately on urgent requests
2. **Verify independently**: Contact the sender through known channels
3. **Check URLs carefully**: Look for typos or suspicious domains
4. **Consult with IT**: When in doubt, ask your security team

### Investigation Steps
- Check the full email headers
- Use online tools to verify suspicious links
- Cross-reference with known phishing databases
- Look up the sender's information independently
- Verify any claims made in the email

## Response Procedures

### If You Suspect Phishing
1. **Don't click**: Avoid clicking links or downloading attachments
2. **Don't reply**: Never respond to suspected phishing emails
3. **Report it**: Forward to your IT security team
4. **Delete safely**: Remove the email after reporting
5. **Warn others**: Alert colleagues if it's a widespread attack

### If You've Been Compromised
1. **Change passwords immediately**: Start with affected accounts
2. **Enable MFA**: Add extra security layers
3. **Report the incident**: Notify your security team
4. **Monitor accounts**: Watch for suspicious activity
5. **Learn from it**: Understand how the attack succeeded",
                    Order = 2,
                    ModuleId = modules[2].Id,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Lessons.AddRange(module3Lessons);
            await context.SaveChangesAsync();

            // Now create quizzes and questions for the lessons
            await SeedQuizzesAndQuestionsAsync(context, module1Lessons, module2Lessons, module3Lessons);
        }

        private static async Task SeedQuizzesAndQuestionsAsync(ApplicationDbContext context, 
            List<Lesson> module1Lessons, List<Lesson> module2Lessons, List<Lesson> module3Lessons)
        {
            // Quiz for Cybersecurity Fundamentals - Lesson 1
            var quiz1 = new Quiz
            {
                Title = "Cybersecurity Basics Assessment",
                Description = "Test your understanding of basic cybersecurity concepts and the CIA triad",
                LessonId = module1Lessons[0].Id,
                PassingScore = 80,
                DateCreated = DateTime.UtcNow
            };

            var quiz2 = new Quiz
            {
                Title = "Cyber Threats Knowledge Check",
                Description = "Evaluate your knowledge of different types of cyber threats and their characteristics",
                LessonId = module1Lessons[1].Id,
                PassingScore = 75,
                DateCreated = DateTime.UtcNow
            };

            var quiz3 = new Quiz
            {
                Title = "Password Security Quiz",
                Description = "Assess your understanding of password security best practices",
                LessonId = module2Lessons[0].Id,
                PassingScore = 85,
                DateCreated = DateTime.UtcNow
            };

            var quiz4 = new Quiz
            {
                Title = "Multi-Factor Authentication Assessment",
                Description = "Test your knowledge of MFA concepts and implementation",
                LessonId = module2Lessons[1].Id,
                PassingScore = 80,
                DateCreated = DateTime.UtcNow
            };

            var quiz5 = new Quiz
            {
                Title = "Email Security Fundamentals Quiz",
                Description = "Evaluate your understanding of email security threats and best practices",
                LessonId = module3Lessons[0].Id,
                PassingScore = 75,
                DateCreated = DateTime.UtcNow
            };

            context.Quizzes.AddRange(quiz1, quiz2, quiz3, quiz4, quiz5);
            await context.SaveChangesAsync();

            // Questions for Quiz 1 - Cybersecurity Basics
            var quiz1Questions = new List<Question>
            {
                new Question
                {
                    Text = "What does the 'C' in the CIA triad stand for?",
                    QuizId = quiz1.Id,
                    Order = 1,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "Which of the following is NOT a common type of malware?",
                    QuizId = quiz1.Id,
                    Order = 2,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "What is the primary goal of cybersecurity?",
                    QuizId = quiz1.Id,
                    Order = 3,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "Which principle ensures that data is accurate and hasn't been tampered with?",
                    QuizId = quiz1.Id,
                    Order = 4,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "What type of attack involves encrypting victim's data and demanding payment?",
                    QuizId = quiz1.Id,
                    Order = 5,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Questions.AddRange(quiz1Questions);
            await context.SaveChangesAsync();

            // Question options for Quiz 1
            var quiz1Options = new List<QuestionOption>
            {
                // Question 1 options
                new QuestionOption { Text = "Confidentiality", IsCorrect = true, QuestionId = quiz1Questions[0].Id, Order = 1 },
                new QuestionOption { Text = "Complexity", IsCorrect = false, QuestionId = quiz1Questions[0].Id, Order = 2 },
                new QuestionOption { Text = "Coordination", IsCorrect = false, QuestionId = quiz1Questions[0].Id, Order = 3 },
                new QuestionOption { Text = "Compliance", IsCorrect = false, QuestionId = quiz1Questions[0].Id, Order = 4 },

                // Question 2 options
                new QuestionOption { Text = "Virus", IsCorrect = false, QuestionId = quiz1Questions[1].Id, Order = 1 },
                new QuestionOption { Text = "Trojan", IsCorrect = false, QuestionId = quiz1Questions[1].Id, Order = 2 },
                new QuestionOption { Text = "Firewall", IsCorrect = true, QuestionId = quiz1Questions[1].Id, Order = 3 },
                new QuestionOption { Text = "Ransomware", IsCorrect = false, QuestionId = quiz1Questions[1].Id, Order = 4 },

                // Question 3 options
                new QuestionOption { Text = "To make systems run faster", IsCorrect = false, QuestionId = quiz1Questions[2].Id, Order = 1 },
                new QuestionOption { Text = "To protect systems and data from digital attacks", IsCorrect = true, QuestionId = quiz1Questions[2].Id, Order = 2 },
                new QuestionOption { Text = "To reduce software costs", IsCorrect = false, QuestionId = quiz1Questions[2].Id, Order = 3 },
                new QuestionOption { Text = "To improve user experience", IsCorrect = false, QuestionId = quiz1Questions[2].Id, Order = 4 },

                // Question 4 options
                new QuestionOption { Text = "Confidentiality", IsCorrect = false, QuestionId = quiz1Questions[3].Id, Order = 1 },
                new QuestionOption { Text = "Integrity", IsCorrect = true, QuestionId = quiz1Questions[3].Id, Order = 2 },
                new QuestionOption { Text = "Availability", IsCorrect = false, QuestionId = quiz1Questions[3].Id, Order = 3 },
                new QuestionOption { Text = "Authentication", IsCorrect = false, QuestionId = quiz1Questions[3].Id, Order = 4 },

                // Question 5 options
                new QuestionOption { Text = "Phishing", IsCorrect = false, QuestionId = quiz1Questions[4].Id, Order = 1 },
                new QuestionOption { Text = "Social Engineering", IsCorrect = false, QuestionId = quiz1Questions[4].Id, Order = 2 },
                new QuestionOption { Text = "Ransomware", IsCorrect = true, QuestionId = quiz1Questions[4].Id, Order = 3 },
                new QuestionOption { Text = "SQL Injection", IsCorrect = false, QuestionId = quiz1Questions[4].Id, Order = 4 }
            };

            // Questions for Quiz 3 - Password Security
            var quiz3Questions = new List<Question>
            {
                new Question
                {
                    Text = "What is the minimum recommended length for a strong password?",
                    QuizId = quiz3.Id,
                    Order = 1,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "Which of these is the WORST password practice?",
                    QuizId = quiz3.Id,
                    Order = 2,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "What is a passphrase?",
                    QuizId = quiz3.Id,
                    Order = 3,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "What type of attack tries common passwords against many accounts?",
                    QuizId = quiz3.Id,
                    Order = 4,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Questions.AddRange(quiz3Questions);
            await context.SaveChangesAsync();

            var quiz3Options = new List<QuestionOption>
            {
                // Question 1 options
                new QuestionOption { Text = "8 characters", IsCorrect = false, QuestionId = quiz3Questions[0].Id, Order = 1 },
                new QuestionOption { Text = "10 characters", IsCorrect = false, QuestionId = quiz3Questions[0].Id, Order = 2 },
                new QuestionOption { Text = "12 characters", IsCorrect = true, QuestionId = quiz3Questions[0].Id, Order = 3 },
                new QuestionOption { Text = "16 characters", IsCorrect = false, QuestionId = quiz3Questions[0].Id, Order = 4 },

                // Question 2 options
                new QuestionOption { Text = "Using a password manager", IsCorrect = false, QuestionId = quiz3Questions[1].Id, Order = 1 },
                new QuestionOption { Text = "Using the same password for all accounts", IsCorrect = true, QuestionId = quiz3Questions[1].Id, Order = 2 },
                new QuestionOption { Text = "Using long passwords", IsCorrect = false, QuestionId = quiz3Questions[1].Id, Order = 3 },
                new QuestionOption { Text = "Including special characters", IsCorrect = false, QuestionId = quiz3Questions[1].Id, Order = 4 },

                // Question 3 options
                new QuestionOption { Text = "A very complex password with symbols", IsCorrect = false, QuestionId = quiz3Questions[2].Id, Order = 1 },
                new QuestionOption { Text = "A combination of multiple random words", IsCorrect = true, QuestionId = quiz3Questions[2].Id, Order = 2 },
                new QuestionOption { Text = "A password written down on paper", IsCorrect = false, QuestionId = quiz3Questions[2].Id, Order = 3 },
                new QuestionOption { Text = "A password that expires frequently", IsCorrect = false, QuestionId = quiz3Questions[2].Id, Order = 4 },

                // Question 4 options
                new QuestionOption { Text = "Brute force attack", IsCorrect = false, QuestionId = quiz3Questions[3].Id, Order = 1 },
                new QuestionOption { Text = "Dictionary attack", IsCorrect = false, QuestionId = quiz3Questions[3].Id, Order = 2 },
                new QuestionOption { Text = "Password spraying", IsCorrect = true, QuestionId = quiz3Questions[3].Id, Order = 3 },
                new QuestionOption { Text = "Credential stuffing", IsCorrect = false, QuestionId = quiz3Questions[3].Id, Order = 4 }
            };

            // Questions for Quiz 5 - Email Security
            var quiz5Questions = new List<Question>
            {
                new Question
                {
                    Text = "What is the most common email-based attack method?",
                    QuizId = quiz5.Id,
                    Order = 1,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "Which of these is a red flag in a suspicious email?",
                    QuizId = quiz5.Id,
                    Order = 2,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "What should you do if you receive a suspicious email?",
                    QuizId = quiz5.Id,
                    Order = 3,
                    DateCreated = DateTime.UtcNow
                },
                new Question
                {
                    Text = "What is spear phishing?",
                    QuizId = quiz5.Id,
                    Order = 4,
                    DateCreated = DateTime.UtcNow
                }
            };

            context.Questions.AddRange(quiz5Questions);
            await context.SaveChangesAsync();

            var quiz5Options = new List<QuestionOption>
            {
                // Question 1 options
                new QuestionOption { Text = "Malware attachments", IsCorrect = false, QuestionId = quiz5Questions[0].Id, Order = 1 },
                new QuestionOption { Text = "Phishing", IsCorrect = true, QuestionId = quiz5Questions[0].Id, Order = 2 },
                new QuestionOption { Text = "Spam", IsCorrect = false, QuestionId = quiz5Questions[0].Id, Order = 3 },
                new QuestionOption { Text = "Email spoofing", IsCorrect = false, QuestionId = quiz5Questions[0].Id, Order = 4 },

                // Question 2 options
                new QuestionOption { Text = "Professional email signature", IsCorrect = false, QuestionId = quiz5Questions[1].Id, Order = 1 },
                new QuestionOption { Text = "Urgent deadline with threats", IsCorrect = true, QuestionId = quiz5Questions[1].Id, Order = 2 },
                new QuestionOption { Text = "Proper company branding", IsCorrect = false, QuestionId = quiz5Questions[1].Id, Order = 3 },
                new QuestionOption { Text = "Clear contact information", IsCorrect = false, QuestionId = quiz5Questions[1].Id, Order = 4 },

                // Question 3 options
                new QuestionOption { Text = "Reply immediately to clarify", IsCorrect = false, QuestionId = quiz5Questions[2].Id, Order = 1 },
                new QuestionOption { Text = "Forward it to all colleagues", IsCorrect = false, QuestionId = quiz5Questions[2].Id, Order = 2 },
                new QuestionOption { Text = "Report it to IT security and delete", IsCorrect = true, QuestionId = quiz5Questions[2].Id, Order = 3 },
                new QuestionOption { Text = "Click the links to investigate", IsCorrect = false, QuestionId = quiz5Questions[2].Id, Order = 4 },

                // Question 4 options
                new QuestionOption { Text = "Mass email phishing to many recipients", IsCorrect = false, QuestionId = quiz5Questions[3].Id, Order = 1 },
                new QuestionOption { Text = "Targeted phishing against specific individuals", IsCorrect = true, QuestionId = quiz5Questions[3].Id, Order = 2 },
                new QuestionOption { Text = "Phishing through social media", IsCorrect = false, QuestionId = quiz5Questions[3].Id, Order = 3 },
                new QuestionOption { Text = "Phishing using phone calls", IsCorrect = false, QuestionId = quiz5Questions[3].Id, Order = 4 }
            };

            // Combine all question options
            var allOptions = new List<QuestionOption>();
            allOptions.AddRange(quiz1Options);
            allOptions.AddRange(quiz3Options);
            allOptions.AddRange(quiz5Options);

            context.QuestionOptions.AddRange(allOptions);
            await context.SaveChangesAsync();
        }
    }
}
