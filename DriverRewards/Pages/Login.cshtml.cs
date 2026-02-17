using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(ApplicationDbContext context, ILogger<LoginModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [Display(Name = "Account type")]
    public string Role { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? StatusMessage { get; private set; }

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
            StatusMessage = "Please fix the errors and try again.";
            return Page();
        }

        var normalizedRole = Role.Trim();
        if (!string.Equals(normalizedRole, "Driver", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedRole, "Sponsor", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Please choose a valid role.";
            return Page();
        }

        var email = Email.Trim();
        var password = Password;

        if (string.Equals(normalizedRole, "Driver", StringComparison.OrdinalIgnoreCase))
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Email == email);

            if (driver == null || !BCrypt.Net.BCrypt.Verify(password, driver.PasswordHash))
            {
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            driver.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await SignInAsync("Driver", driver.DriverId.ToString(), driver.Username, driver.Email);
            return RedirectToPage("/Index");
        }

        if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == email);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            admin.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var displayName = string.IsNullOrWhiteSpace(admin.DisplayName) ? "Admin" : admin.DisplayName;
            await SignInAsync("Admin", admin.AdminId.ToString(), displayName, admin.Email);
            return RedirectToPage("/Admin/Dashboard");
        }

        var sponsor = await _context.Sponsors
            .FirstOrDefaultAsync(s => s.Email == email);

        if (sponsor == null || !BCrypt.Net.BCrypt.Verify(password, sponsor.PasswordHash))
        {
            StatusMessage = "Invalid email or password.";
            return Page();
        }

        if (!sponsor.IsApproved)
        {
            StatusMessage = "Your sponsor account is pending admin approval.";
            return Page();
        }

        sponsor.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SignInAsync("Sponsor", sponsor.SponsorId.ToString(), sponsor.Name, sponsor.Email);
        return RedirectToPage("/Index");
    }

    private async Task SignInAsync(string role, string id, string displayName, string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, $"{role}:{id}"),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        _logger.LogInformation("{Role} signed in with email {Email}.", role, email);
    }

}
