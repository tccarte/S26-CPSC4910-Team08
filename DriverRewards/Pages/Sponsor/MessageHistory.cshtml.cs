using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class MessageHistoryModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MessageHistoryModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<MessageHistoryItem> Messages { get; set; } = new();

    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    [StringLength(500)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sponsor = await GetCurrentSponsorAsync();
        if (sponsor == null)
        {
            return Challenge();
        }

        IQueryable<DriverNotification> query = _context.DriverNotifications
            .AsNoTracking()
            .Include(n => n.Driver)
            .Where(n =>
                n.Type == "SponsorMessage" &&
                n.Driver.Sponsor == sponsor.Name);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var trimmedSearch = Search.Trim();
            query = query.Where(n => n.Message.Contains(trimmedSearch));
        }

        if (FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(FromDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
            query = query.Where(n => n.CreatedAt >= fromUtc);
        }

        if (ToDate.HasValue)
        {
            var toExclusiveUtc = DateTime.SpecifyKind(ToDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            query = query.Where(n => n.CreatedAt < toExclusiveUtc);
        }

        Messages = await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new MessageHistoryItem
            {
                NotificationId = n.NotificationId,
                DriverId = n.DriverId,
                DriverName = (n.Driver.FirstName + " " + n.Driver.LastName).Trim(),
                DriverEmail = n.Driver.Email,
                Message = n.Message,
                CreatedAtUtc = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync();

        foreach (var message in Messages)
        {
            message.CreatedAtLocal = DateTime.SpecifyKind(message.CreatedAtUtc, DateTimeKind.Utc).ToLocalTime();
        }

        TotalCount = Messages.Count;
        return Page();
    }

    private async Task<Models.Sponsor?> GetCurrentSponsorAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return null;
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Sponsor", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!int.TryParse(parts[1], out var sponsorId))
        {
            return null;
        }

        return await _context.Sponsors
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
    }

    public class MessageHistoryItem
    {
        public int NotificationId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime CreatedAtLocal { get; set; }
        public bool IsRead { get; set; }
    }
}