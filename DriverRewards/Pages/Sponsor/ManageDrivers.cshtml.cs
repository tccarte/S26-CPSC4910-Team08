using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverEntity = DriverRewards.Models.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class ManageDriversModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ManageDriversModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<DriverEntity> Drivers { get; set; } = new();
    public string? StatusMessage { get; set; }

    [BindProperty]
    [Required]
    [StringLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary Password")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sponsorName = GetCurrentSponsorName();
        if (string.IsNullOrWhiteSpace(sponsorName))
        {
            return Challenge();
        }

        await LoadDriversAsync(sponsorName);

        if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var sponsorName = GetCurrentSponsorName();
        if (string.IsNullOrWhiteSpace(sponsorName))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadDriversAsync(sponsorName);
            return Page();
        }

        var normalizedEmail = Email.Trim().ToLowerInvariant();
        var trimmedUsername = Username.Trim();
        var trimmedFirstName = FirstName.Trim();
        var trimmedLastName = LastName.Trim();
        var trimmedSponsorName = sponsorName.Trim();

        var emailExistsInOrg = await _context.Drivers.AsNoTracking()
            .AnyAsync(d =>
                d.Sponsor == trimmedSponsorName &&
                d.Email.ToLower() == normalizedEmail);

        if (emailExistsInOrg)
        {
            ModelState.AddModelError(nameof(Email), "A driver with this email already exists in your organization.");
            await LoadDriversAsync(trimmedSponsorName);
            return Page();
        }

        var usernameExistsInOrg = await _context.Drivers.AsNoTracking()
            .AnyAsync(d =>
                d.Sponsor == trimmedSponsorName &&
                d.Username.ToLower() == trimmedUsername.ToLower());

        if (usernameExistsInOrg)
        {
            ModelState.AddModelError(nameof(Username), "A driver with this username already exists in your organization.");
            await LoadDriversAsync(trimmedSponsorName);
            return Page();
        }

        var driver = new DriverEntity
        {
            Username = trimmedUsername,
            FirstName = trimmedFirstName,
            LastName = trimmedLastName,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            Sponsor = trimmedSponsorName,
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            CreatedAt = DateTime.UtcNow,
            NumPoints = 0
        };

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Driver '{driver.FirstName} {driver.LastName}' created successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int driverId)
    {
        var sponsorName = GetCurrentSponsorName();
        if (string.IsNullOrWhiteSpace(sponsorName))
        {
            return Challenge();
        }

        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.DriverId == driverId && d.Sponsor == sponsorName);

        if (driver != null)
        {
            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Driver '{driver.FirstName} {driver.LastName}' deleted.";
        }

        return RedirectToPage();
    }

    private async Task LoadDriversAsync(string sponsorName)
    {
        Drivers = await _context.Drivers.AsNoTracking()
            .Where(d => d.Sponsor == sponsorName)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    private string? GetCurrentSponsorName()
    {
        return User.FindFirstValue(ClaimTypes.Name);
    }
}