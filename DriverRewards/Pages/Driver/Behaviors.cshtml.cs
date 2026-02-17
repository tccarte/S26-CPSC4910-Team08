using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver;

[Authorize(Roles = "Driver")]
public class BehaviorsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public BehaviorsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<BehaviorItem> BehaviorList { get; set; } = new();

    public async Task OnGetAsync()
    {
        BehaviorList = await _context.Behaviors.AsNoTracking()
            .OrderByDescending(b => b.Points)
            .Select(b => new BehaviorItem
            {
                Name = b.Name,
                Description = b.Description,
                Points = b.Points
            })
            .ToListAsync();
    }

    public class BehaviorItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Points { get; set; }
    }
}
