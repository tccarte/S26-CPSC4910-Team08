using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Data;
using Microsoft.EntityFrameworkCore;
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

        [BindProperty]
        public SponsorEntity Sponsor { get; set; } = default!;

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var normalizedName = Sponsor.Name.Trim().ToLower();

            var nameExists = await _context.Sponsors
                .AnyAsync(s => s.Name.ToLower() == normalizedName);

            if (nameExists)
            {
                ModelState.AddModelError("Sponsor.Name", "A sponsor with this name already exists.");
                return Page();
            }

            Sponsor.Name = Sponsor.Name.Trim();
            Sponsor.Email = Sponsor.Email.Trim();

            _context.Sponsors.Add(Sponsor);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
