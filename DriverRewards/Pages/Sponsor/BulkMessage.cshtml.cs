using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class BulkMessageModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notifications;

    public BulkMessageModel(ApplicationDbContext context, NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    [BindProperty]
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
    public string Message { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public int DriverCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sponsor = await GetCurrentSponsorAsync();
        if (sponsor == null) return Challenge();

        DriverCount = await _context.Drivers
            .CountAsync(d => d.Sponsor == sponsor.Name);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sponsor = await GetCurrentSponsorAsync();
        if (sponsor == null) return Challenge();

        DriverCount = await _context.Drivers
            .CountAsync(d => d.Sponsor == sponsor.Name);

        if (!ModelState.IsValid)
            return Page();

        var drivers = await _context.Drivers
            .Where(d => d.Sponsor == sponsor.Name)
            .ToListAsync();

        foreach (var driver in drivers)
            await _notifications.SendBulkMessageAsync(driver, Message, _context);

        StatusMessage = $"Message sent to {drivers.Count} driver{(drivers.Count == 1 ? "" : "s")}.";
        return RedirectToPage();
    }

    private async Task<Models.Sponsor?> GetCurrentSponsorAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim)) return null;

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Sponsor", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!int.TryParse(parts[1], out var sponsorId)) return null;

        return await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
    }
}
