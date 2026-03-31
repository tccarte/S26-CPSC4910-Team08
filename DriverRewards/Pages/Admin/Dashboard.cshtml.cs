using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public DashboardModel(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public List<DriverRow> Drivers { get; private set; } = new();
    public List<SponsorRow> Sponsors { get; private set; } = new();
    public List<RequestRow> PendingRequests { get; private set; } = new();
    public List<PendingSponsorRow> PendingSponsors { get; private set; } = new();
    public List<AuditPreviewRow> RecentAuditEntries { get; private set; } = new();
    public int AuditEntriesLast24Hours { get; private set; }
    public int FailedAuditEntriesLast24Hours { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var drivers = await _context.Drivers.AsNoTracking()
            .OrderBy(d => d.Username)
            .ToListAsync();

        var sponsors = await _context.Sponsors.AsNoTracking()
            .Where(s => s.IsApproved)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var pendingSponsors = await _context.Sponsors.AsNoTracking()
            .Where(s => !s.IsApproved)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var requests = await _context.SponsorChangeRequests.AsNoTracking()
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var auditSince = DateTime.UtcNow.AddHours(-24);
        AuditEntriesLast24Hours = await _context.AuditLogs.AsNoTracking()
            .CountAsync(a => a.OccurredAt >= auditSince);
        FailedAuditEntriesLast24Hours = await _context.AuditLogs.AsNoTracking()
            .CountAsync(a => a.OccurredAt >= auditSince && (a.Action.Contains("Failed") || a.Action.Contains("Blocked")));
        RecentAuditEntries = await _context.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.OccurredAt)
            .Take(8)
            .Select(a => new AuditPreviewRow
            {
                OccurredAt = FormatDateTime(a.OccurredAt),
                Category = a.Category,
                Action = a.Action,
                Actor = string.IsNullOrWhiteSpace(a.ActorType)
                    ? "System"
                    : $"{a.ActorType} {a.ActorName ?? a.ActorId ?? "Unknown"}",
                Description = a.Description ?? string.Empty
            })
            .ToListAsync();

        var driverLookup = drivers.ToDictionary(d => d.DriverId, d => d);

        Drivers = drivers.Select(d => new DriverRow
        {
            DriverId = d.DriverId,
            Username = d.Username,
            Email = d.Email,
            Sponsor = d.Sponsor,
            FedexId = d.FedexId,
            CreatedAt = FormatDateTime(d.CreatedAt),
            LastLoginAt = FormatDateTime(d.LastLoginAt),
            Points = d.NumPoints ?? 0
        }).ToList();

        Sponsors = sponsors.Select(s => new SponsorRow
        {
            SponsorId = s.SponsorId,
            Name = s.Name,
            Email = s.Email,
            Phone = s.Phone,
            DollarToPointRatio = s.DollarToPointRatio,
            CreatedAt = FormatDateTime(s.CreatedAt),
            LastLoginAt = FormatDateTime(s.LastLoginAt)
        }).ToList();

        PendingSponsors = pendingSponsors.Select(s => new PendingSponsorRow
        {
            SponsorId = s.SponsorId,
            Name = s.Name,
            Email = s.Email,
            Phone = s.Phone,
            CreatedAt = FormatDateTime(s.CreatedAt)
        }).ToList();

        PendingRequests = requests.Select(r =>
        {
            driverLookup.TryGetValue(r.DriverId, out var driver);
            return new RequestRow
            {
                DriverName = driver?.Username ?? "Unknown",
                DriverEmail = driver?.Email ?? "Unknown",
                CurrentSponsor = r.CurrentSponsor,
                RequestedSponsor = r.RequestedSponsor,
                CreatedAt = r.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy"),
                Note = r.Note ?? string.Empty
            };
        }).ToList();
    }

    public class DriverRow
    {
        public int DriverId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sponsor { get; set; } = string.Empty;
        public string? FedexId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string LastLoginAt { get; set; } = string.Empty;
        public int Points { get; set; }
    }

    public class SponsorRow
    {
        public int SponsorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal DollarToPointRatio { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string LastLoginAt { get; set; } = string.Empty;
    }

    public class PendingSponsorRow
    {
        public int SponsorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RequestRow
    {
        public string DriverName { get; set; } = string.Empty;
        public string DriverEmail { get; set; } = string.Empty;
        public string CurrentSponsor { get; set; } = string.Empty;
        public string RequestedSponsor { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class AuditPreviewRow
    {
        public string OccurredAt { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostApproveSponsorAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        sponsor.IsApproved = true;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectSponsorAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        _context.Sponsors.Remove(sponsor);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveDriverAsync(int driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        if (driver == null)
        {
            return NotFound();
        }

        _context.Drivers.Remove(driver);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddDriverPointsAsync(int driverId, int pointsToAdd)
    {
        if (pointsToAdd <= 0)
        {
            StatusMessage = "Points to add must be greater than 0.";
            return RedirectToPage();
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        if (driver == null)
        {
            return NotFound();
        }

        driver.NumPoints = (driver.NumPoints ?? 0) + pointsToAdd;
        await _context.SaveChangesAsync();
        await _auditService.LogEventAsync(
            category: "Points",
            action: "AdminAdjustment",
            description: $"Admin added {pointsToAdd} points to {driver.Username}.",
            entityType: "Driver",
            entityId: driver.DriverId.ToString(),
            changes: new
            {
                AddedPoints = pointsToAdd,
                NewPoints = driver.NumPoints ?? 0
            });

        StatusMessage = $"Added {pointsToAdd} points to {driver.Username}. New total: {driver.NumPoints}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveSponsorAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        _context.Sponsors.Remove(sponsor);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private static string FormatDateTime(DateTime? utcValue)
    {
        if (utcValue == null)
        {
            return "Never";
        }

        var local = DateTime.SpecifyKind(utcValue.Value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("MM/dd/yyyy HH:mm:ss");
    }
}
