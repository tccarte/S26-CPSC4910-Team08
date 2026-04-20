using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SponsorReportModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SponsorReportModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int? SponsorId { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public List<SponsorOption> SponsorOptions { get; private set; } = new();
    public List<SponsorReportRow> ReportRows { get; private set; } = new();

    public int TotalSponsors { get; private set; }
    public int SuspendedSponsors { get; private set; }
    public int ApprovedSponsors { get; private set; }
    public int AuditEventCount { get; private set; }
    public DateTime MaxSelectableDate => DateTime.Today;
    public string? ValidationMessage { get; private set; }

    public async Task OnGetAsync()
    {
        SponsorOptions = await _context.Sponsors.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SponsorOption
            {
                SponsorId = s.SponsorId,
                Name = s.Name
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

        IQueryable<DriverRewards.Models.Sponsor> sponsorQuery = _context.Sponsors.AsNoTracking();

        if (SponsorId.HasValue)
        {
            sponsorQuery = sponsorQuery.Where(s => s.SponsorId == SponsorId.Value);
        }

        var sponsors = await sponsorQuery
            .OrderBy(s => s.Name)
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
            .Where(a => a.EntityType == "Sponsor")
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync();

        var filteredSponsorIds = auditEntries
            .Where(a => int.TryParse(a.EntityId, out _))
            .Select(a => int.Parse(a.EntityId!))
            .Distinct()
            .ToHashSet();

        var hasDateFilter = StartDate.HasValue || EndDate.HasValue;

        if (hasDateFilter)
        {
            sponsors = sponsors
                .Where(s => filteredSponsorIds.Contains(s.SponsorId))
                .ToList();
        }

        TotalSponsors = sponsors.Count;
        SuspendedSponsors = sponsors.Count(s => s.IsSuspended);
        ApprovedSponsors = sponsors.Count(s => s.IsApproved);

        var displayedSponsorIds = sponsors.Select(s => s.SponsorId).ToHashSet();

        AuditEventCount = auditEntries.Count(a =>
            a.EntityId != null &&
            int.TryParse(a.EntityId, out var entitySponsorId) &&
            displayedSponsorIds.Contains(entitySponsorId));

        ReportRows = sponsors.Select(s =>
        {
            var sponsorAuditEntries = auditEntries
                .Where(a => a.EntityId == s.SponsorId.ToString())
                .ToList();

            var latestAudit = sponsorAuditEntries
                .OrderByDescending(a => a.OccurredAt)
                .FirstOrDefault();

            return new SponsorReportRow
            {
                SponsorId = s.SponsorId,
                Name = s.Name,
                Email = s.Email,
                IsApproved = s.IsApproved,
                IsSuspended = s.IsSuspended,
                CreatedAt = FormatDateTime(s.CreatedAt),
                LastLoginAt = FormatDateTime(s.LastLoginAt),
                AuditEventsInRange = sponsorAuditEntries.Count,
                LatestAuditAction = latestAudit?.Action ?? "None",
                LatestAuditAt = latestAudit == null ? "Never" : FormatDateTime(latestAudit.OccurredAt)
            };
        }).ToList();
    }

    private static string FormatDateTime(DateTime? utcValue)
    {
        if (utcValue == null)
            return "Never";

        var local = DateTime.SpecifyKind(utcValue.Value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("MM/dd/yyyy HH:mm:ss");
    }

    public class SponsorOption
    {
        public int SponsorId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SponsorReportRow
    {
        public int SponsorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsSuspended { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string LastLoginAt { get; set; } = string.Empty;
        public int AuditEventsInRange { get; set; }
        public string LatestAuditAction { get; set; } = "None";
        public string LatestAuditAt { get; set; } = "Never";
    }
}