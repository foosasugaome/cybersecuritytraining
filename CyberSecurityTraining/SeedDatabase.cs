using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CyberSecurityTraining.Data;
using CyberSecurityTraining.Models;
using CyberSecurityTraining.Services;

namespace CyberSecurityTraining
{
    public class SeedDatabase
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ICertificateService, CertificateService>();
            builder.Services.AddScoped<IProgressService, ProgressService>();
            builder.Services.AddScoped<IMarkdownService, MarkdownService>();

            var app = builder.Build();

            Console.WriteLine("Starting database seeding...");

            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    await SeedData.InitializeAsync(scope.ServiceProvider);
                }
                Console.WriteLine("Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during seeding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
