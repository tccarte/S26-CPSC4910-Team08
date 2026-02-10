using System.Security.Claims;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver;

[Authorize(Roles = "Driver")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string DisplayName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Sponsor { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string JoinedOn { get; private set; } = string.Empty;
    public string LastLoginAt { get; private set; } = string.Empty;
    public int Points { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Challenge();
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Driver", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!int.TryParse(parts[1], out var driverId))
        {
            return Forbid();
        }

        var driver = await _context.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DriverId == driverId);

        if (driver == null)
        {
            return NotFound();
        }

        DisplayName = string.IsNullOrWhiteSpace(driver.FirstName)
            ? driver.Username
            : driver.FirstName;
        Username = driver.Username;
        Email = driver.Email;
        Sponsor = driver.Sponsor;
        Phone = driver.Phone;
        JoinedOn = driver.CreatedAt.ToLocalTime().ToString("MM/dd/yyyy");
        LastLoginAt = driver.LastLoginAt == null
            ? "Never"
            : DateTime.SpecifyKind(driver.LastLoginAt.Value, DateTimeKind.Utc)
                .ToLocalTime()
                .ToString("MM/dd/yyyy HH:mm:ss");
        Points = driver.NumPoints ?? 0;

        return Page();
    }
}
