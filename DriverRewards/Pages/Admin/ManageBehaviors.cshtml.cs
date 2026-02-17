using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BehaviorEntity = DriverRewards.Models.Behavior;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ManageBehaviorsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ManageBehaviorsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<BehaviorEntity> Behaviors { get; set; } = new();
    public string? StatusMessage { get; set; }

    [BindProperty]
    [Required]
    [StringLength(100)]
    [Display(Name = "Behavior Name")]
    public string BehaviorName { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [BindProperty]
    [Required]
    [Display(Name = "Points")]
    public int Points { get; set; }

    public async Task OnGetAsync()
    {
        await LoadBehaviors();
        if (TempData["StatusMessage"] != null)
            StatusMessage = TempData["StatusMessage"]?.ToString();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadBehaviors();
            return Page();
        }

        var behavior = new BehaviorEntity
        {
            Name = BehaviorName.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Points = Points,
            CreatedAt = DateTime.UtcNow
        };

        _context.Behaviors.Add(behavior);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Behavior '{BehaviorName.Trim()}' added successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int behaviorId)
    {
        var behavior = await _context.Behaviors.FindAsync(behaviorId);
        if (behavior != null)
        {
            _context.Behaviors.Remove(behavior);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Behavior '{behavior.Name}' deleted.";
        }

        return RedirectToPage();
    }

    private async Task LoadBehaviors()
    {
        Behaviors = await _context.Behaviors.AsNoTracking()
            .OrderByDescending(b => b.Points)
            .ToListAsync();
    }
}
