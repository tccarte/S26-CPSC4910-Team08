using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Services;
using System.Security.Claims;

namespace DriverRewards.Pages;

public class LogoutModel : PageModel
{
    private readonly AuditService _auditService;
    private readonly SessionService _sessionService;

    public LogoutModel(AuditService auditService, SessionService sessionService)
    {
        _auditService = auditService;
        _sessionService = sessionService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sessionId = User.FindFirstValue("sid");
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _sessionService.RevokeSessionAsync(sessionId, "User logged out.");
        }

        await _auditService.LogEventAsync(
            category: "Authentication",
            action: "Logout",
            description: "User signed out.");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }
}
