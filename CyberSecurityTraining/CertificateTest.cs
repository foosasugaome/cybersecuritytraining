using System;
using System.IO;
using System.Threading.Tasks;
using CyberSecurityTraining.Services;
using CyberSecurityTraining.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CyberSecurityTraining.Tests
{
    public class CertificateTest
    {
        public static async Task TestCertificateGeneration()
        {
            var logger = NullLogger<CertificateService>.Instance;
            var certificateService = new CertificateService(logger);
            
            // Create a test user
            var testUser = new ApplicationUser
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };
            
            // Create a test module
            var testModule = new Module
            {
                Id = 1,
                Title = "Test Security Module",
                Description = "A test module for certificate generation"
            };
            
            try
            {
                Console.WriteLine("Testing certificate generation...");
                
                // Test individual certificate generation
                var certificateBytes = await certificateService.GenerateCertificateAsync(testUser, testModule);
                
                if (certificateBytes != null && certificateBytes.Length > 0)
                {
                    Console.WriteLine($"✅ Certificate generated successfully! Size: {certificateBytes.Length} bytes");
                    
                    // Save to file for testing
                    var testFilePath = "test_certificate.pdf";
                    await File.WriteAllBytesAsync(testFilePath, certificateBytes);
                    Console.WriteLine($"✅ Certificate saved to: {testFilePath}");
                }
                else
                {
                    Console.WriteLine("❌ Certificate generation failed - empty result");
                }
                
                // Test comprehensive certificate generation
                var modules = new[] { testModule };
                var comprehensiveCertificateBytes = await certificateService.GenerateCompletionCertificateAsync(testUser, modules);
                
                if (comprehensiveCertificateBytes != null && comprehensiveCertificateBytes.Length > 0)
                {
                    Console.WriteLine($"✅ Comprehensive certificate generated successfully! Size: {comprehensiveCertificateBytes.Length} bytes");
                    
                    // Save to file for testing
                    var testFilePath = "test_comprehensive_certificate.pdf";
                    await File.WriteAllBytesAsync(testFilePath, comprehensiveCertificateBytes);
                    Console.WriteLine($"✅ Comprehensive certificate saved to: {testFilePath}");
                }
                else
                {
                    Console.WriteLine("❌ Comprehensive certificate generation failed - empty result");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during certificate generation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
