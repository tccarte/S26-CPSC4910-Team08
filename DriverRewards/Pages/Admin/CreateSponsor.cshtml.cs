using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Data;
using SponsorEntity = DriverRewards.Models.Sponsor;

namespace DriverRewards.Pages.Admin
{
    public class CreateSponsorModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateSponsorModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public SponsorEntity Sponsor { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Sponsors.Add(Sponsor);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
