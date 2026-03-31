using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Services;

namespace DriverRewards.Pages;

public class LogoutModel : PageModel
{
    private readonly AuditService _auditService;

    public LogoutModel(AuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
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
