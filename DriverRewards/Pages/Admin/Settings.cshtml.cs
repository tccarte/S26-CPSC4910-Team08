using System.Security.Claims;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DriverRewards.Services;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly SessionService _sessionService;

    public SettingsModel(ApplicationDbContext context, SessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
    }

    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var admin = await LoadAdminAsync();
        if (admin == null) return NotFound();

        DisplayName = admin.DisplayName;
        Email = admin.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var admin = await LoadAdminAsync();
        if (admin == null) return NotFound();

        _context.Admins.Remove(admin);
        await _context.SaveChangesAsync();
        await _sessionService.RevokeAllSessionsAsync("Admin", admin.AdminId, "Admin account deleted.");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }

    private async Task<Models.Admin?> LoadAdminAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim)) return null;

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Admin", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!int.TryParse(parts[1], out var adminId)) return null;

        return await _context.Admins.FirstOrDefaultAsync(a => a.AdminId == adminId);
    }
}
