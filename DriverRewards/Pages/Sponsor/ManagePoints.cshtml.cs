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

        Drivers = await _context.Drivers.AsNoTracking()
            .Where(d => d.Sponsor == sponsorName)
            .OrderBy(d => d.Username)
            .Select(d => new DriverRow
            {
                DriverId = d.DriverId,
                Username = d.Username,
                Email = d.Email,
                Points = d.NumPoints ?? 0
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

        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.DriverId == DriverId && d.Sponsor == sponsorName);

        if (driver == null)
        {
            TempData["StatusMessage"] = "Driver not found.";
            return RedirectToPage();
        }

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

    public class DriverRow
    {
        public int DriverId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Points { get; set; }
    }
}
