using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CreateDriverModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateDriverModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public DriverInput Input { get; set; } = new();

    public List<SelectListItem> SponsorOptions { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSponsorOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadSponsorOptionsAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var username = Input.Username.Trim();
        var email = Input.Email.Trim();
        var sponsorName = Input.Sponsor.Trim();

        var usernameExists = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Username == username);
        if (usernameExists)
        {
            ModelState.AddModelError("Input.Username", "Username is already taken.");
            return Page();
        }

        var emailExists = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Email == email);
        if (emailExists)
        {
            ModelState.AddModelError("Input.Email", "Email is already registered.");
            return Page();
        }

        var sponsorExists = await _context.Sponsors.AsNoTracking()
            .AnyAsync(s => s.IsApproved && s.Name == sponsorName);
        if (!sponsorExists)
        {
            ModelState.AddModelError("Input.Sponsor", "Select an approved sponsor.");
            return Page();
        }

        var driver = new DriverRewards.Models.Driver
        {
            Username = username,
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            Sponsor = sponsorName,
            Phone = string.IsNullOrWhiteSpace(Input.Phone) ? null : Input.Phone.Trim(),
            FedexId = string.IsNullOrWhiteSpace(Input.FedexId) ? null : Input.FedexId.Trim(),
            NumPoints = Input.Points,
            IsApproved = Input.ApproveImmediately,
            MustResetPassword = Input.RequirePasswordReset,
            CreatedAt = DateTime.UtcNow
        };

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        StatusMessage = $"Driver account for '{driver.Username}' created successfully.";
        return RedirectToPage("/Admin/Dashboard");
    }

    private async Task LoadSponsorOptionsAsync()
    {
        SponsorOptions = await _context.Sponsors.AsNoTracking()
            .Where(s => s.IsApproved)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Name,
                Text = s.Name
            })
            .ToListAsync();
    }

    public class DriverInput
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

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

        [Required]
        [StringLength(50)]
        [Display(Name = "Sponsor")]
        public string Sponsor { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(50)]
        [Display(Name = "FedEx ID")]
        public string? FedexId { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Starting Points")]
        public int Points { get; set; }

        [Display(Name = "Approve account immediately")]
        public bool ApproveImmediately { get; set; } = true;

        [Display(Name = "Require password reset on first login")]
        public bool RequirePasswordReset { get; set; } = true;
    }
}
