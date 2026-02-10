using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DriverRewards.Data;
using DriverRewards.Models;

namespace DriverRewards.Pages.Admin
{
    public class ManageSponsorsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageSponsorsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Sponsor> Sponsors { get; set; } = default!;

        
        public async Task OnGetAsync()
        {
            if (_context.Sponsors != null)
            {
                Sponsors = await _context.Sponsors.ToListAsync();
            }
        }

        
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var sponsor = await _context.Sponsors.FindAsync(id);

            if (sponsor != null)
            {
                _context.Sponsors.Remove(sponsor);
                await _context.SaveChangesAsync();
            }

            
            return RedirectToPage();
        }
    }
}