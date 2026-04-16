using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class ManagePointsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly AuditService _auditService;

    public ManagePointsModel(ApplicationDbContext context, NotificationService notificationService, AuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    public List<DriverRow> Drivers { get; set; } = new();
    public List<DriverRow> PendingDrivers { get; set; } = new();
    public string? StatusMessage { get; set; }

    [BindProperty]
    public int DriverId { get; set; }

    [BindProperty]
    public int PointChange { get; set; }

    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var sponsorName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(sponsorName))
            return Challenge();

        Drivers = await _context.DriverSponsors.AsNoTracking()
            .Where(ds => ds.SponsorName == sponsorName && ds.IsApproved)
            .OrderBy(ds => ds.Driver.Username)
            .Select(ds => new DriverRow
            {
                DriverId = ds.Driver.DriverId,
                Username = ds.Driver.Username,
                Email = ds.Driver.Email,
                Points = ds.Driver.NumPoints ?? 0
            })
            .ToListAsync();

        PendingDrivers = await _context.DriverSponsors.AsNoTracking()
            .Where(ds => ds.SponsorName == sponsorName && !ds.IsApproved)
            .OrderBy(ds => ds.JoinedAt)
            .Select(ds => new DriverRow
            {
                DriverId = ds.Driver.DriverId,
                Username = ds.Driver.Username,
                Email = ds.Driver.Email,
                Points = 0
            })
            .ToListAsync();

        if (TempData["StatusMessage"] != null)
            StatusMessage = TempData["StatusMessage"]?.ToString();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sponsorName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(sponsorName))
            return Challenge();

        var driverSponsor = await _context.DriverSponsors
            .Include(ds => ds.Driver)
            .FirstOrDefaultAsync(ds => ds.DriverId == DriverId && ds.SponsorName == sponsorName && ds.IsApproved);

        if (driverSponsor == null)
        {
            TempData["StatusMessage"] = "Driver not found.";
            return RedirectToPage();
        }

        var driver = driverSponsor.Driver;
        var previousPoints = driver.NumPoints ?? 0;
        driver.NumPoints = (driver.NumPoints ?? 0) + PointChange;

        await _context.SaveChangesAsync();
        await _auditService.LogEventAsync(
            category: "Points",
            action: "SponsorAdjustment",
            description: $"{sponsorName} adjusted {driver.Username} by {PointChange} points.",
            entityType: "Driver",
            entityId: driver.DriverId.ToString(),
            changes: new
            {
                PreviousPoints = previousPoints,
                NewPoints = driver.NumPoints ?? 0,
                PointChange
            },
            metadata: new
            {
                Sponsor = sponsorName,
                Reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim()
            });

        await _notificationService.NotifyPointsChangedAsync(driver, PointChange, _context);

        var action = PointChange >= 0 ? "Added" : "Subtracted";
        TempData["StatusMessage"] = $"{action} {Math.Abs(PointChange)} points for {driver.Username}. New total: {driver.NumPoints}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveDriverAsync(int driverId)
    {
        var sponsorName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(sponsorName)) return Challenge();

        var driverSponsor = await _context.DriverSponsors
            .Include(ds => ds.Driver)
            .FirstOrDefaultAsync(ds => ds.DriverId == driverId && ds.SponsorName == sponsorName && !ds.IsApproved);
        if (driverSponsor == null) return RedirectToPage();

        driverSponsor.IsApproved = true;

        // Activate the driver account if this is their first approval
        if (!driverSponsor.Driver.IsApproved)
            driverSponsor.Driver.IsApproved = true;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{driverSponsor.Driver.Username}'s account has been approved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectDriverAsync(int driverId)
    {
        var sponsorName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(sponsorName)) return Challenge();

        var driverSponsor = await _context.DriverSponsors
            .Include(ds => ds.Driver)
            .FirstOrDefaultAsync(ds => ds.DriverId == driverId && ds.SponsorName == sponsorName && !ds.IsApproved);
        if (driverSponsor == null) return RedirectToPage();

        var driver = driverSponsor.Driver;
        _context.DriverSponsors.Remove(driverSponsor);

        // Only remove the driver account entirely if they have no other sponsor relationships
        var hasOtherSponsors = await _context.DriverSponsors
            .AnyAsync(ds => ds.DriverId == driverId && ds.Id != driverSponsor.Id);
        if (!hasOtherSponsors)
            _context.Drivers.Remove(driver);

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{driver.Username}'s account has been rejected and removed.";
        return RedirectToPage();
    }

    public class DriverRow
    {
        public int DriverId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Points { get; set; }
    }
}
