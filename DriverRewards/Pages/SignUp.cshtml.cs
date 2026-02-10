using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Data;
using DriverEntity = DriverRewards.Models.Driver;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages;

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
    [Required]
    [StringLength(50)]
    [Display(Name = "Sponsor")]
    public string Sponsor { get; set; } = string.Empty;

    [BindProperty]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number (Optional)")]
    public string? Phone { get; set; }

    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        if (TempData["StatusMessage"] != null)
        {
            StatusMessage = TempData["StatusMessage"]?.ToString();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usernameExists = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Username == Username);
        if (usernameExists)
        {
            ModelState.AddModelError("Username", "Username is already taken");
            return Page();
        }

        var emailExists = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Email == Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email is already registered");
            return Page();
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

        var driver = new DriverEntity
        {
            Username = Username.Trim(),
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Email = Email.Trim(),
            PasswordHash = passwordHash,
            Sponsor = Sponsor.Trim(),
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            CreatedAt = DateTime.UtcNow,
            NumPoints = 0
        };

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Driver account created successfully! Please log in.";
        return RedirectToPage("/Login");
    }
}
