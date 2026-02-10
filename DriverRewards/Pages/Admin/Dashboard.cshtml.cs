using DriverRewards.Data;
using DriverRewards.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<DriverRow> Drivers { get; private set; } = new();
    public List<SponsorRow> Sponsors { get; private set; } = new();
    public List<RequestRow> PendingRequests { get; private set; } = new();
    public List<PendingSponsorRow> PendingSponsors { get; private set; } = new();

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
