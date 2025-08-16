using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using CyberSecurityTraining.Models;

namespace CyberSecurityTraining.Services
{
    public interface ICertificateService
    {
        Task<byte[]> GenerateCertificateAsync(ApplicationUser user, Module module);
        Task<byte[]> GenerateCompletionCertificateAsync(ApplicationUser user, IEnumerable<Module> completedModules);
    }

    public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;

        public CertificateService(ILogger<CertificateService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateCertificateAsync(ApplicationUser user, Module module)
        {
            return await Task.FromResult(GeneratePdfCertificate(
                $"{user.FirstName} {user.LastName}",
                module.Title,
                DateTime.UtcNow
            ));
        }

        public async Task<byte[]> GenerateCompletionCertificateAsync(ApplicationUser user, IEnumerable<Module> completedModules)
        {
            var moduleNames = string.Join(", ", completedModules.Select(m => m.Title));
            return await Task.FromResult(GeneratePdfCertificate(
                $"{user.FirstName} {user.LastName}",
                $"Cybersecurity Training - Modules: {moduleNames}",
                DateTime.UtcNow
            ));
        }

        private byte[] GeneratePdfCertificate(string userName, string courseName, DateTime completionDate)
        {
            using var memoryStream = new MemoryStream();
            
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Get fonts
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Certificate header
            var title = new Paragraph("CERTIFICATE OF COMPLETION")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(boldFont)
                .SetFontSize(24);
            document.Add(title);

            document.Add(new Paragraph("\n"));

            // User name
            var nameText = new Paragraph("This is to certify that")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(normalFont)
                .SetFontSize(14);
            document.Add(nameText);

            var name = new Paragraph(userName)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(boldFont)
                .SetFontSize(20);
            document.Add(name);

            // Course completion
            var completionText = new Paragraph("has successfully completed the training course:")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(normalFont)
                .SetFontSize(14);
            document.Add(completionText);

            var course = new Paragraph(courseName)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(boldFont)
                .SetFontSize(16);
            document.Add(course);

            document.Add(new Paragraph("\n"));

            // Completion date
            var dateText = new Paragraph($"Completed on: {completionDate:MMMM dd, yyyy}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(normalFont)
                .SetFontSize(12);
            document.Add(dateText);

            document.Add(new Paragraph("\n\n"));

            // Signature line
            var signature = new Paragraph("_________________________")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(normalFont);
            document.Add(signature);

            var signatureLabel = new Paragraph("Authorized Signature")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(signatureLabel);

            document.Close();

            _logger.LogInformation($"Generated certificate for {userName} - {courseName}");

            return memoryStream.ToArray();
        }
    }
}
