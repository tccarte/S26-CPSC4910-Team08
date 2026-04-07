using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public ChangePasswordModel(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsRequiredReset { get; private set; }
    public string? StatusMessage { get; private set; }

    public IActionResult OnGet(bool required = false)
    {
        IsRequiredReset = required;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool required = false)
    {
        IsRequiredReset = required;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!TryGetIdentity(out var role, out var userId))
        {
            return Unauthorized();
        }

        var result = role switch
        {
            "Driver" => await UpdateDriverPasswordAsync(userId),
            "Sponsor" => await UpdateSponsorPasswordAsync(userId),
            "Admin" => await UpdateAdminPasswordAsync(userId),
            _ => (false, "/")
        };

        if (!result.Item1)
        {
            ModelState.AddModelError(nameof(CurrentPassword), "Current password is incorrect.");
            return Page();
        }

        await _auditService.LogEventAsync(
            category: "Authentication",
            action: "PasswordChanged",
            description: $"{role} changed account password.",
            entityType: role,
            entityId: userId.ToString());

        TempData["StatusMessage"] = "Password updated successfully.";
        return RedirectToPage(result.Item2);
    }

    private async Task<(bool, string)> UpdateDriverPasswordAsync(int driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        if (driver == null || !BCrypt.Net.BCrypt.Verify(CurrentPassword, driver.PasswordHash))
        {
            return (false, "/ChangePassword");
        }

        driver.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        driver.MustResetPassword = false;
        driver.FailedLoginAttempts = 0;
        driver.LockoutEndUtc = null;
        await _context.SaveChangesAsync();
        return (true, "/Driver/Dashboard");
    }

    private async Task<(bool, string)> UpdateSponsorPasswordAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null || !BCrypt.Net.BCrypt.Verify(CurrentPassword, sponsor.PasswordHash))
        {
            return (false, "/ChangePassword");
        }

        sponsor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        sponsor.MustResetPassword = false;
        sponsor.FailedLoginAttempts = 0;
        sponsor.LockoutEndUtc = null;
        await _context.SaveChangesAsync();
        return (true, "/Sponsor/ManagePoints");
    }

    private async Task<(bool, string)> UpdateAdminPasswordAsync(int adminId)
    {
        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminId == adminId);
        if (admin == null || !BCrypt.Net.BCrypt.Verify(CurrentPassword, admin.PasswordHash))
        {
            return (false, "/ChangePassword");
        }

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        admin.MustResetPassword = false;
        admin.FailedLoginAttempts = 0;
        admin.LockoutEndUtc = null;
        await _context.SaveChangesAsync();
        return (true, "/Admin/Dashboard");
    }

    private bool TryGetIdentity(out string role, out int userId)
    {
        role = string.Empty;
        userId = 0;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return false;
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out userId))
        {
            return false;
        }

        role = parts[0];
        return true;
    }
}
