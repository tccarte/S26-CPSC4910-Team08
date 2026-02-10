using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using SponsorEntity = DriverRewards.Models.Sponsor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

public class SignUpModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SignUpModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    [Required]
    [StringLength(50)]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [BindProperty]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Support Phone")]
    public string? SupportPhone { get; set; }

    [BindProperty]
    [StringLength(255)]
    [Display(Name = "Headquarters Address")]
    public string? HeadquartersAddress { get; set; }

    [BindProperty]
    [Range(0.01, 100000)]
    [Display(Name = "Dollar-to-Point Ratio")]
    public decimal DollarToPointRatio { get; set; } = 1.00m;

    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var emailExists = await _context.Sponsors.AsNoTracking()
            .AnyAsync(s => s.Email == Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return Page();
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

        var sponsor = new SponsorEntity
        {
            Name = Name.Trim(),
            Email = Email.Trim(),
            PasswordHash = passwordHash,
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            SupportPhone = string.IsNullOrWhiteSpace(SupportPhone) ? null : SupportPhone.Trim(),
            HeadquartersAddress = string.IsNullOrWhiteSpace(HeadquartersAddress) ? null : HeadquartersAddress.Trim(),
            DollarToPointRatio = DollarToPointRatio,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sponsors.Add(sponsor);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Sponsor account created successfully! Please log in.";
        return RedirectToPage("/Login");
    }
}
