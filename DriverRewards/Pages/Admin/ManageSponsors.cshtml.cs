using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DriverRewards.Data;
using DriverRewards.Models;

namespace DriverRewards.Pages.Admin;

public class ManageSponsorsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ManageSponsorsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Sponsor> Sponsors { get; set; } = default!;

    [TempData]
    public string StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Sponsors = await _context.Sponsors.ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var sponsor = await _context.Sponsors.FindAsync(id);

        if (sponsor != null)
        {
            _context.Sponsors.Remove(sponsor);
            await _context.SaveChangesAsync();
            StatusMessage = $"Sponsor '{sponsor.Name}' has been deleted.";
        }
        else
        {
            StatusMessage = "Error: Sponsor not found.";
        }

        return RedirectToPage();
    }
}