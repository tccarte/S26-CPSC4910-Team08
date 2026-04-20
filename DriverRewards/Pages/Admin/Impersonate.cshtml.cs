using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ImpersonateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly SessionService _sessionService;
    private readonly ILogger<ImpersonateModel> _logger;

    public ImpersonateModel(
        ApplicationDbContext context,
        SessionService sessionService,
        ILogger<ImpersonateModel> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    public List<DriverRewards.Models.Driver> Drivers { get; set; } = new();
    public List<DriverRewards.Models.Sponsor> Sponsors { get; set; } = new();

    public async Task OnGetAsync()
    {
        Drivers = await _context.Drivers
            .OrderBy(d => d.Username)
            .Take(50)
            .ToListAsync();

        Sponsors = await _context.Sponsors
            .OrderBy(s => s.Name)
            .Take(50)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDriverAsync(int driverId)
    {
        var driver = await _context.Drivers.FindAsync(driverId);
        if (driver == null) return RedirectToPage();

        return await SignInAs("Driver", driver.DriverId, driver.Username, driver.Email);
    }

    public async Task<IActionResult> OnPostSponsorAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FindAsync(sponsorId);
        if (sponsor == null) return RedirectToPage();

        return await SignInAs("Sponsor", sponsor.SponsorId, sponsor.Name, sponsor.Email);
    }

    public async Task<IActionResult> OnPostStopAsync()
    {
        var originalId = User.FindFirst("OriginalUserId")?.Value;
        var originalRole = User.FindFirst("OriginalRole")?.Value;
        var originalName = User.FindFirst("OriginalName")?.Value;
        var originalEmail = User.FindFirst("OriginalEmail")?.Value;

        if (originalId == null || originalRole == null)
            return RedirectToPage();

        var parts = originalId.Split(':');
        var userId = int.Parse(parts[1]);

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await SignInAsync(originalRole, userId, originalName ?? "Admin", originalEmail ?? "", userAgent);

        return RedirectToPage("/Admin/Dashboard");
    }

    private async Task<IActionResult> SignInAs(string role, int userId, string name, string email)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var currentName = User.FindFirst(ClaimTypes.Name)?.Value;
        var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Sign in as target user
        await SignInAsync(role, userId, name, email, userAgent);

        // NOTE: Original identity is not preserved across full session recreation.
        // This keeps behavior consistent with your existing auth system.

        return RedirectToPage("/Index");
    }

    private async Task SignInAsync(string role, int userId, string displayName, string email, string? userAgent)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _sessionService.CreateSessionAsync(
            role: role,
            userId: userId,
            sessionId: sessionId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            expiresAtUtc: expiresAtUtc);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, $"{role}:{userId}"),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("sid", sessionId)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = expiresAtUtc
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        _logger.LogInformation("Impersonation sign-in as {Role}:{UserId}", role, userId);
    }
}