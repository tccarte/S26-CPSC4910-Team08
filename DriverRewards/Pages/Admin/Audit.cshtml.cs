using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AuditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActorType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public List<string> Categories { get; private set; } = new();
    public List<string> ActorTypes { get; private set; } = new();
    public List<AuditRow> AuditEntries { get; private set; } = new();
    public int TotalEntries { get; private set; }
    public int EntriesLast24Hours { get; private set; }
    public int DataChangesLast24Hours { get; private set; }
    public int FailedEventsLast24Hours { get; private set; }

    public async Task OnGetAsync()
    {
        Categories = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.Category != null)
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ActorTypes = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.ActorType != null)
            .Select(a => a.ActorType!)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var since = DateTime.UtcNow.AddHours(-24);
        TotalEntries = await _context.AuditLogs.AsNoTracking().CountAsync();
        EntriesLast24Hours = await _context.AuditLogs.AsNoTracking().CountAsync(a => a.OccurredAt >= since);
        DataChangesLast24Hours = await _context.AuditLogs.AsNoTracking().CountAsync(a => a.Category == "DataChange" && a.OccurredAt >= since);
        FailedEventsLast24Hours = await _context.AuditLogs.AsNoTracking().CountAsync(a =>
            a.OccurredAt >= since &&
            (a.Action.Contains("Failed") || a.Action.Contains("Blocked")));

        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Category))
        {
            query = query.Where(a => a.Category == Category);
        }

        if (!string.IsNullOrWhiteSpace(ActorType))
        {
            query = query.Where(a => a.ActorType == ActorType);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(a =>
                (a.Description != null && a.Description.Contains(search)) ||
                (a.EntityType != null && a.EntityType.Contains(search)) ||
                (a.EntityId != null && a.EntityId.Contains(search)) ||
                (a.ActorName != null && a.ActorName.Contains(search)) ||
                (a.ActorId != null && a.ActorId.Contains(search)) ||
                (a.Action != null && a.Action.Contains(search)) ||
                (a.IpAddress != null && a.IpAddress.Contains(search)));
        }

        AuditEntries = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(200)
            .Select(a => new AuditRow
            {
                OccurredAt = a.OccurredAt,
                Category = a.Category,
                Action = a.Action,
                Actor = string.IsNullOrWhiteSpace(a.ActorType)
                    ? "System"
                    : $"{a.ActorType} {a.ActorName ?? a.ActorId ?? "Unknown"}",
                Entity = string.IsNullOrWhiteSpace(a.EntityType)
                    ? "-"
                    : $"{a.EntityType} {a.EntityId ?? string.Empty}".Trim(),
                Description = a.Description ?? string.Empty,
                RequestPath = a.RequestPath ?? string.Empty,
                IpAddress = a.IpAddress ?? string.Empty,
                ChangesJson = a.ChangesJson,
                MetadataJson = a.MetadataJson
            })
            .ToListAsync();
    }

    public class AuditRow
    {
        public DateTime OccurredAt { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? ChangesJson { get; set; }
        public string? MetadataJson { get; set; }
    }
}
