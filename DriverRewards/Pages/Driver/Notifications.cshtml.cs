using System.Security.Claims;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver;

[Authorize(Roles = "Driver")]
public class NotificationsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public NotificationsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<NotificationRow> History { get; set; } = new();

    [BindProperty]
    public bool NotifyEmailPoints { get; set; }

    [BindProperty]
    public bool NotifySmsPoints { get; set; }

    [BindProperty]
    public bool NotifyEmailOrder { get; set; }

    [BindProperty]
    public bool NotifySmsOrder { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var driver = await GetCurrentDriverAsync();
        if (driver == null) return Challenge();

        NotifyEmailPoints = driver.NotifyEmailPoints;
        NotifySmsPoints = driver.NotifySmsPoints;
        NotifyEmailOrder = driver.NotifyEmailOrder;
        NotifySmsOrder = driver.NotifySmsOrder;

        History = await _context.DriverNotifications
            .Where(n => n.DriverId == driver.DriverId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationRow
            {
                Type = n.Type,
                Message = n.Message,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var driver = await GetCurrentDriverAsync();
        if (driver == null) return Challenge();

        driver.NotifyEmailPoints = NotifyEmailPoints;
        driver.NotifySmsPoints = NotifySmsPoints;
        driver.NotifyEmailOrder = NotifyEmailOrder;
        driver.NotifySmsOrder = NotifySmsOrder;

        await _context.SaveChangesAsync();

        StatusMessage = "Notification preferences saved.";
        return RedirectToPage();
    }

    private async Task<Models.Driver?> GetCurrentDriverAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim)) return null;

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Driver", StringComparison.OrdinalIgnoreCase)) return null;

        if (!int.TryParse(parts[1], out var driverId)) return null;

        return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
    }

    public class NotificationRow
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string TypeLabel => Type switch
        {
            "PointsChanged" => "Points Update",
            "OrderPlaced" => "Order Placed",
            _ => Type
        };

        public string BadgeClass => Type switch
        {
            "PointsChanged" => "text-bg-success",
            "OrderPlaced" => "text-bg-primary",
            _ => "text-bg-secondary"
        };
    }
}
