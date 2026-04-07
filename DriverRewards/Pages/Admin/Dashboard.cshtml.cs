using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using System.Security.Claims;
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
    private readonly SessionService _sessionService;

    public DashboardModel(ApplicationDbContext context, AuditService auditService, SessionService sessionService)
    {
        _context = context;
        _auditService = auditService;
        _sessionService = sessionService;
    }

    public List<DriverRow> Drivers { get; private set; } = new();
    public List<SponsorRow> Sponsors { get; private set; } = new();
    public List<AdminRow> Admins { get; private set; } = new();
    public List<ActiveSessionRow> ActiveSessions { get; private set; } = new();
    public List<RequestRow> PendingRequests { get; private set; } = new();
    public List<PendingSponsorRow> PendingSponsors { get; private set; } = new();
    public List<AuditPreviewRow> RecentAuditEntries { get; private set; } = new();
    public int AuditEntriesLast24Hours { get; private set; }
    public int FailedAuditEntriesLast24Hours { get; private set; }
    public int NewIpEventsLast24Hours { get; private set; }
    public int LockedAccountCount { get; private set; }

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

        var admins = await _context.Admins.AsNoTracking()
            .OrderBy(a => a.DisplayName)
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
        NewIpEventsLast24Hours = await _context.AuditLogs.AsNoTracking()
            .CountAsync(a => a.OccurredAt >= auditSince && a.Action == "LoginRiskNewIp");
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
                Description = a.Description ?? string.Empty,
                IpAddress = a.IpAddress ?? string.Empty
            })
            .ToListAsync();
        LockedAccountCount = drivers.Count(d => d.LockoutEndUtc.HasValue && d.LockoutEndUtc > DateTime.UtcNow)
            + sponsors.Count(s => s.LockoutEndUtc.HasValue && s.LockoutEndUtc > DateTime.UtcNow)
            + admins.Count(a => a.LockoutEndUtc.HasValue && a.LockoutEndUtc > DateTime.UtcNow);

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
            LastLoginIp = d.LastLoginIp ?? "-",
            LastFailedLoginIp = d.LastFailedLoginIp ?? "-",
            FailedLoginAttempts = d.FailedLoginAttempts,
            LockoutEndAt = FormatDateTime(d.LockoutEndUtc),
            IsSuspended = d.IsSuspended,
            MustResetPassword = d.MustResetPassword,
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
            LastLoginAt = FormatDateTime(s.LastLoginAt),
            LastLoginIp = s.LastLoginIp ?? "-",
            LastFailedLoginIp = s.LastFailedLoginIp ?? "-",
            FailedLoginAttempts = s.FailedLoginAttempts,
            LockoutEndAt = FormatDateTime(s.LockoutEndUtc),
            IsSuspended = s.IsSuspended,
            MustResetPassword = s.MustResetPassword
        }).ToList();

        Admins = admins.Select(a => new AdminRow
        {
            AdminId = a.AdminId,
            DisplayName = string.IsNullOrWhiteSpace(a.DisplayName) ? "Admin" : a.DisplayName,
            Email = a.Email,
            CreatedAt = FormatDateTime(a.CreatedAt),
            LastLoginAt = FormatDateTime(a.LastLoginAt),
            LastLoginIp = a.LastLoginIp ?? "-",
            LastFailedLoginIp = a.LastFailedLoginIp ?? "-",
            FailedLoginAttempts = a.FailedLoginAttempts,
            LockoutEndAt = FormatDateTime(a.LockoutEndUtc),
            IsSuspended = a.IsSuspended,
            MustResetPassword = a.MustResetPassword,
            IsCurrentAdmin = TryGetCurrentAdminId(out var currentAdminId) && currentAdminId == a.AdminId
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

        ActiveSessions = await _context.UserSessions.AsNoTracking()
            .Where(s => !s.IsRevoked && s.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(s => s.LastSeenAtUtc ?? s.CreatedAtUtc)
            .Take(100)
            .Select(s => new ActiveSessionRow
            {
                SessionId = s.SessionId,
                Role = s.Role,
                UserId = s.UserId,
                IpAddress = string.IsNullOrWhiteSpace(s.IpAddress) ? "-" : s.IpAddress!,
                UserAgent = string.IsNullOrWhiteSpace(s.UserAgent) ? "-" : s.UserAgent!,
                CreatedAt = FormatDateTime(s.CreatedAtUtc),
                LastSeenAt = FormatDateTime(s.LastSeenAtUtc),
                ExpiresAt = FormatDateTime(s.ExpiresAtUtc)
            })
            .ToListAsync();
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
        public string LastLoginIp { get; set; } = "-";
        public string LastFailedLoginIp { get; set; } = "-";
        public int FailedLoginAttempts { get; set; }
        public string LockoutEndAt { get; set; } = "Never";
        public bool IsSuspended { get; set; }
        public bool MustResetPassword { get; set; }
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
        public string LastLoginIp { get; set; } = "-";
        public string LastFailedLoginIp { get; set; } = "-";
        public int FailedLoginAttempts { get; set; }
        public string LockoutEndAt { get; set; } = "Never";
        public bool IsSuspended { get; set; }
        public bool MustResetPassword { get; set; }
    }

    public class AdminRow
    {
        public int AdminId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string LastLoginAt { get; set; } = string.Empty;
        public string LastLoginIp { get; set; } = "-";
        public string LastFailedLoginIp { get; set; } = "-";
        public int FailedLoginAttempts { get; set; }
        public string LockoutEndAt { get; set; } = "Never";
        public bool IsSuspended { get; set; }
        public bool MustResetPassword { get; set; }
        public bool IsCurrentAdmin { get; set; }
    }

    public class ActiveSessionRow
    {
        public string SessionId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string LastSeenAt { get; set; } = string.Empty;
        public string ExpiresAt { get; set; } = string.Empty;
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
        public string IpAddress { get; set; } = string.Empty;
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

        await _sessionService.RevokeAllSessionsAsync("Driver", driver.DriverId, "Driver account removed by admin.");
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

        await _sessionService.RevokeAllSessionsAsync("Sponsor", sponsor.SponsorId, "Sponsor account removed by admin.");
        _context.Sponsors.Remove(sponsor);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSuspendDriverAsync(int driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        if (driver == null)
        {
            return NotFound();
        }

        driver.IsSuspended = true;
        driver.SuspendedAtUtc = DateTime.UtcNow;
        driver.SuspensionReason = "Suspended by admin.";
        await _context.SaveChangesAsync();
        await _sessionService.RevokeAllSessionsAsync("Driver", driver.DriverId, "Driver suspended by admin.");
        StatusMessage = $"Suspended driver {driver.Username}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnsuspendDriverAsync(int driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        if (driver == null)
        {
            return NotFound();
        }

        driver.IsSuspended = false;
        driver.SuspendedAtUtc = null;
        driver.SuspensionReason = null;
        await _context.SaveChangesAsync();
        StatusMessage = $"Unsuspended driver {driver.Username}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForceDriverResetAsync(int driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        if (driver == null)
        {
            return NotFound();
        }

        driver.MustResetPassword = true;
        await _context.SaveChangesAsync();
        StatusMessage = $"Driver {driver.Username} must reset password at next request.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSuspendSponsorAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        sponsor.IsSuspended = true;
        sponsor.SuspendedAtUtc = DateTime.UtcNow;
        sponsor.SuspensionReason = "Suspended by admin.";
        await _context.SaveChangesAsync();
        await _sessionService.RevokeAllSessionsAsync("Sponsor", sponsor.SponsorId, "Sponsor suspended by admin.");
        StatusMessage = $"Suspended sponsor {sponsor.Name}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnsuspendSponsorAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        sponsor.IsSuspended = false;
        sponsor.SuspendedAtUtc = null;
        sponsor.SuspensionReason = null;
        await _context.SaveChangesAsync();
        StatusMessage = $"Unsuspended sponsor {sponsor.Name}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForceSponsorResetAsync(int sponsorId)
    {
        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        sponsor.MustResetPassword = true;
        await _context.SaveChangesAsync();
        StatusMessage = $"Sponsor {sponsor.Name} must reset password at next request.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSuspendAdminAsync(int adminId)
    {
        if (!TryGetCurrentAdminId(out var currentAdminId) || currentAdminId == adminId)
        {
            StatusMessage = "You cannot suspend your own admin account.";
            return RedirectToPage();
        }

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminId == adminId);
        if (admin == null)
        {
            return NotFound();
        }

        admin.IsSuspended = true;
        admin.SuspendedAtUtc = DateTime.UtcNow;
        admin.SuspensionReason = "Suspended by admin.";
        await _context.SaveChangesAsync();
        await _sessionService.RevokeAllSessionsAsync("Admin", admin.AdminId, "Admin account suspended by another admin.");
        StatusMessage = $"Suspended admin {admin.DisplayName}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnsuspendAdminAsync(int adminId)
    {
        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminId == adminId);
        if (admin == null)
        {
            return NotFound();
        }

        admin.IsSuspended = false;
        admin.SuspendedAtUtc = null;
        admin.SuspensionReason = null;
        await _context.SaveChangesAsync();
        StatusMessage = $"Unsuspended admin {admin.DisplayName}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForceAdminResetAsync(int adminId)
    {
        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminId == adminId);
        if (admin == null)
        {
            return NotFound();
        }

        admin.MustResetPassword = true;
        await _context.SaveChangesAsync();
        StatusMessage = $"Admin {admin.DisplayName} must reset password at next request.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeSessionsAsync(string role, int userId)
    {
        if (string.IsNullOrWhiteSpace(role) || userId <= 0)
        {
            return BadRequest();
        }

        var revokedCount = await _sessionService.RevokeAllSessionsAsync(role.Trim(), userId, "Revoked by admin.");
        StatusMessage = revokedCount == 0
            ? "No active sessions found."
            : $"Revoked {revokedCount} active session(s).";
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

    private bool TryGetCurrentAdminId(out int adminId)
    {
        adminId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return false;
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(parts[1], out adminId);
    }
}
