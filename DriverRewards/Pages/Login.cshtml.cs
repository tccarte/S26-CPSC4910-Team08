using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Services;
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
    private readonly AuditService _auditService;

    public LoginModel(ApplicationDbContext context, ILogger<LoginModel> logger, AuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
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
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginFailed",
                    description: $"Failed driver login for {email}.",
                    entityType: "Driver",
                    changes: new { Email = email, Role = "Driver" });
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            if (!driver.IsApproved)
            {
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginBlocked",
                    description: $"Unapproved driver login blocked for {driver.Username}.",
                    entityType: "Driver",
                    entityId: driver.DriverId.ToString(),
                    metadata: new { driver.Email });
                StatusMessage = "Your account is pending approval from your sponsor.";
                return Page();
            }

            driver.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginSucceeded",
                description: $"Driver {driver.Username} signed in.",
                entityType: "Driver",
                entityId: driver.DriverId.ToString(),
                metadata: new { driver.Email });

            await SignInAsync("Driver", driver.DriverId.ToString(), driver.Username, driver.Email);
            return RedirectToPage("/Driver/Dashboard");
        }

        if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == email);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginFailed",
                    description: $"Failed admin login for {email}.",
                    entityType: "Admin",
                    changes: new { Email = email, Role = "Admin" });
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            var displayName = string.IsNullOrWhiteSpace(admin.DisplayName) ? "Admin" : admin.DisplayName;
            admin.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginSucceeded",
                description: $"Admin {displayName} signed in.",
                entityType: "Admin",
                entityId: admin.AdminId.ToString(),
                metadata: new { admin.Email });

            await SignInAsync("Admin", admin.AdminId.ToString(), displayName, admin.Email);
            return RedirectToPage("/Admin/Dashboard");
        }

        var sponsor = await _context.Sponsors
            .FirstOrDefaultAsync(s => s.Email == email);

        if (sponsor == null || !BCrypt.Net.BCrypt.Verify(password, sponsor.PasswordHash))
        {
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginFailed",
                description: $"Failed sponsor login for {email}.",
                entityType: "Sponsor",
                changes: new { Email = email, Role = "Sponsor" });
            StatusMessage = "Invalid email or password.";
            return Page();
        }

        if (!sponsor.IsApproved)
        {
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginBlocked",
                description: $"Unapproved sponsor login blocked for {sponsor.Name}.",
                entityType: "Sponsor",
                entityId: sponsor.SponsorId.ToString(),
                metadata: new { sponsor.Email, sponsor.IsApproved });
            StatusMessage = "Your sponsor account is pending admin approval.";
            return Page();
        }

        sponsor.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogEventAsync(
            category: "Authentication",
            action: "LoginSucceeded",
            description: $"Sponsor {sponsor.Name} signed in.",
            entityType: "Sponsor",
            entityId: sponsor.SponsorId.ToString(),
            metadata: new { sponsor.Email });

        await SignInAsync("Sponsor", sponsor.SponsorId.ToString(), sponsor.Name, sponsor.Email);
        return RedirectToPage("/Sponsor/ManagePoints");
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

