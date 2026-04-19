using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DriverReportModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DriverReportModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int? DriverId { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public List<DriverOption> DriverOptions { get; private set; } = new();
    public List<DriverReportRow> ReportRows { get; private set; } = new();

    public int TotalDrivers { get; private set; }
    public int TotalPoints { get; private set; }
    public int SuspendedDrivers { get; private set; }
    public int ApprovedDrivers { get; private set; }
    public int AuditEventCount { get; private set; }
    public DateTime MaxSelectableDate => DateTime.Today;
    public string? ValidationMessage { get; private set; }

    public async Task OnGetAsync()
    {
        DriverOptions = await _context.Drivers.AsNoTracking()
            .OrderBy(d => d.Username)
            .Select(d => new DriverOption
            {
                DriverId = d.DriverId,
                Username = d.Username
            })
            .ToListAsync();

        var today = DateTime.Today;

        if (StartDate.HasValue && StartDate.Value.Date > today)
        {
            StartDate = today;
            ValidationMessage = "Start date cannot be later than today.";
        }

        if (EndDate.HasValue && EndDate.Value.Date > today)
        {
            EndDate = today;
            ValidationMessage = "End date cannot be later than today.";
        }

        IQueryable<DriverRewards.Models.Driver> driverQuery = _context.Drivers.AsNoTracking();

        if (DriverId.HasValue)
        {
            driverQuery = driverQuery.Where(d => d.DriverId == DriverId.Value);
        }

        var drivers = await driverQuery
            .OrderBy(d => d.Username)
            .ToListAsync();

        var auditQuery = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (StartDate.HasValue)
        {
            var startUtc = StartDate.Value.Date;
            auditQuery = auditQuery.Where(a => a.OccurredAt >= startUtc);
        }

        if (EndDate.HasValue)
        {
            var endExclusiveUtc = EndDate.Value.Date.AddDays(1);
            auditQuery = auditQuery.Where(a => a.OccurredAt < endExclusiveUtc);
        }

        var auditEntries = await auditQuery
            .Where(a => a.EntityType == "Driver")
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync();

        var filteredDriverIds = auditEntries
            .Where(a => int.TryParse(a.EntityId, out _))
            .Select(a => int.Parse(a.EntityId!))
            .Distinct()
            .ToHashSet();

        var hasDateFilter = StartDate.HasValue || EndDate.HasValue;

        if (hasDateFilter)
        {
            drivers = drivers
                .Where(d => filteredDriverIds.Contains(d.DriverId))
                .ToList();
        }

        TotalDrivers = drivers.Count;
        TotalPoints = drivers.Sum(d => d.NumPoints ?? 0);
        SuspendedDrivers = drivers.Count(d => d.IsSuspended);
        ApprovedDrivers = drivers.Count(d => d.IsApproved);

        var displayedDriverIds = drivers.Select(d => d.DriverId).ToHashSet();

        AuditEventCount = auditEntries.Count(a =>
            a.EntityId != null &&
            int.TryParse(a.EntityId, out var entityDriverId) &&
            displayedDriverIds.Contains(entityDriverId));

        ReportRows = drivers.Select(d =>
        {
            var driverAuditEntries = auditEntries
                .Where(a => a.EntityId == d.DriverId.ToString())
                .ToList();

            var latestAudit = driverAuditEntries
                .OrderByDescending(a => a.OccurredAt)
                .FirstOrDefault();

            return new DriverReportRow
            {
                DriverId = d.DriverId,
                Username = d.Username,
                Email = d.Email,
                Sponsor = string.IsNullOrWhiteSpace(d.Sponsor) ? "-" : d.Sponsor,
                Points = d.NumPoints ?? 0,
                IsApproved = d.IsApproved,
                IsSuspended = d.IsSuspended,
                CreatedAt = FormatDateTime(d.CreatedAt),
                LastLoginAt = FormatDateTime(d.LastLoginAt),
                AuditEventsInRange = driverAuditEntries.Count,
                LatestAuditAction = latestAudit?.Action ?? "None",
                LatestAuditAt = latestAudit == null ? "Never" : FormatDateTime(latestAudit.OccurredAt)
            };
        }).ToList();
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

    public class DriverOption
    {
        public int DriverId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class DriverReportRow
    {
        public int DriverId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sponsor { get; set; } = "-";
        public int Points { get; set; }
        public bool IsApproved { get; set; }
        public bool IsSuspended { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string LastLoginAt { get; set; } = string.Empty;
        public int AuditEventsInRange { get; set; }
        public string LatestAuditAction { get; set; } = "None";
        public string LatestAuditAt { get; set; } = "Never";
    }
}