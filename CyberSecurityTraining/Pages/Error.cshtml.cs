using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CyberSecurityTraining.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public int? ErrorStatusCode { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet(int? statusCode = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ErrorStatusCode = statusCode ?? HttpContext.Response.StatusCode;
        
        // Log the error for monitoring purposes
        _logger.LogWarning("Error page accessed. StatusCode: {StatusCode}, RequestId: {RequestId}, Path: {Path}, User: {User}", 
            ErrorStatusCode, 
            RequestId, 
            HttpContext.Request.Path, 
            User.Identity?.Name ?? "Anonymous");
    }
}

