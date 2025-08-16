using Markdig;

namespace CyberSecurityTraining.Services
{
    public interface IMarkdownService
    {
        string ToHtml(string markdown);
    }

    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownService()
        {
            // Configure the Markdig pipeline with common extensions
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions() // Includes tables, task lists, auto-links, etc.
                .UseSoftlineBreakAsHardlineBreak() // Convert soft line breaks to hard line breaks
                .Build();
        }

        public string ToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            try
            {
                return Markdown.ToHtml(markdown, _pipeline);
            }
            catch (Exception)
            {
                // If markdown parsing fails, return the original content in a safe way
                return $"<p>{System.Net.WebUtility.HtmlEncode(markdown)}</p>";
            }
        }
    }
}
