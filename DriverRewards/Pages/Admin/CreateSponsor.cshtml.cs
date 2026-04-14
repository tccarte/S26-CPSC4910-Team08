using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using DriverRewards.Data;
using Microsoft.EntityFrameworkCore;
using SponsorEntity = DriverRewards.Models.Sponsor;
using System.ComponentModel.DataAnnotations;

namespace DriverRewards.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateSponsorModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateSponsorModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SponsorInput Sponsor { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

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
            var normalizedEmail = Sponsor.Email.Trim().ToLower();

            var nameExists = await _context.Sponsors.AsNoTracking()
                .AnyAsync(s => s.Name.ToLower() == normalizedName);

            if (nameExists)
            {
                ModelState.AddModelError("Sponsor.Name", "A sponsor with this name already exists.");
                return Page();
            }

            var emailExists = await _context.Sponsors.AsNoTracking()
                .AnyAsync(s => s.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                ModelState.AddModelError("Sponsor.Email", "Email is already registered.");
                return Page();
            }

            var sponsor = new SponsorEntity
            {
                Name = Sponsor.Name.Trim(),
                Email = Sponsor.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(Sponsor.Phone) ? null : Sponsor.Phone.Trim(),
                SupportPhone = string.IsNullOrWhiteSpace(Sponsor.SupportPhone) ? null : Sponsor.SupportPhone.Trim(),
                HeadquartersAddress = string.IsNullOrWhiteSpace(Sponsor.HeadquartersAddress) ? null : Sponsor.HeadquartersAddress.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Sponsor.Password),
                DollarToPointRatio = Sponsor.DollarToPointRatio,
                IsApproved = true,
                MustResetPassword = Sponsor.RequirePasswordReset,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sponsors.Add(sponsor);
            await _context.SaveChangesAsync();

            StatusMessage = $"Sponsor account for '{sponsor.Name}' created successfully.";
            return RedirectToPage("/Admin/Dashboard");
        }

        public class SponsorInput
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "Company Name")]
            public string Name { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [StringLength(100)]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Phone]
            [StringLength(20)]
            [Display(Name = "Phone")]
            public string? Phone { get; set; }

            [Phone]
            [StringLength(20)]
            [Display(Name = "Support Phone")]
            public string? SupportPhone { get; set; }

            [StringLength(255)]
            [Display(Name = "Headquarters Address")]
            public string? HeadquartersAddress { get; set; }

            [Required]
            [Range(0.01, 100000)]
            [Display(Name = "Dollar-to-Point Ratio")]
            public decimal DollarToPointRatio { get; set; } = 1;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Temporary Password")]
            public string Password { get; set; } = string.Empty;

            [Required]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Require password reset on first login")]
            public bool RequirePasswordReset { get; set; } = true;
        }
    }
}
