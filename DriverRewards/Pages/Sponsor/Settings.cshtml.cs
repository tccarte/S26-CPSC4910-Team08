using System.Security.Claims;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class SettingsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SettingsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public string SponsorName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var sponsor = await LoadSponsorAsync();
        if (sponsor == null) return NotFound();

        SponsorName = sponsor.Name;
        Email = sponsor.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var sponsor = await LoadSponsorAsync();
        if (sponsor == null) return NotFound();

        _context.Sponsors.Remove(sponsor);
        await _context.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }

    private async Task<Models.Sponsor?> LoadSponsorAsync()
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
