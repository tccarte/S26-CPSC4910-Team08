using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Data;
using DriverRewards.Models;
using System.ComponentModel.DataAnnotations;

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
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if username already exists
        if (_context.Drivers.Any(d => d.Username == Username))
        {
            ModelState.AddModelError("Username", "Username is already taken");
            return Page();
        }

        // Check if email already exists
        if (_context.Drivers.Any(d => d.Email == Email))
        {
            ModelState.AddModelError("Email", "Email is already registered");
            return Page();
        }

        // Hash the password (using BCrypt)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

        // Create new driver
        var driver = new Driver
        {
            Username = Username,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            PasswordHash = passwordHash,
            Sponsor = Sponsor,
            Phone = Phone,
            CreatedAt = DateTime.UtcNow,
            NumPoints = 0
        };

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        // Redirect to login page with success message
        TempData["StatusMessage"] = "Account created successfully! Please log in.";
        return RedirectToPage("/Login");
    }
}
