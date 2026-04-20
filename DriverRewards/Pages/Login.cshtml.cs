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
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoginModel> _logger;
    private readonly AuditService _auditService;
    private readonly SessionService _sessionService;

    public LoginModel(
        ApplicationDbContext context,
        ILogger<LoginModel> logger,
        AuditService auditService,
        SessionService sessionService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
        _sessionService = sessionService;
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

        if (!string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase)
            && MaintenanceState.IsActive())
        {
            StatusMessage = "The site is currently under maintenance. Only admins may sign in.";
            return Page();
        }

        var email = Email.Trim();
        var password = Password;
        var loginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        if (string.Equals(normalizedRole, "Driver", StringComparison.OrdinalIgnoreCase))
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Email == email);

            if (driver == null)
            {
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginFailed",
                    description: $"Failed driver login for {email}.",
                    entityType: "Driver",
                    changes: new { Email = email, Role = "Driver" },
                    metadata: new { IpAddress = loginIp });
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            if (driver.IsSuspended)
            {
                StatusMessage = "This account has been suspended. Contact an administrator.";
                return Page();
            }

            if (driver.LockoutEndUtc.HasValue && driver.LockoutEndUtc.Value > DateTime.UtcNow)
            {
                var localLockoutEnd = DateTime.SpecifyKind(driver.LockoutEndUtc.Value, DateTimeKind.Utc).ToLocalTime();
                StatusMessage = $"Account locked due to failed logins. Try again after {localLockoutEnd:MM/dd/yyyy HH:mm:ss}.";
                return Page();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, driver.PasswordHash))
            {
                driver.LastFailedLoginIp = loginIp;
                driver.FailedLoginAttempts += 1;

                if (driver.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    driver.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                }

                await _context.SaveChangesAsync();
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginFailed",
                    description: $"Failed driver login for {email}.",
                    entityType: "Driver",
                    entityId: driver.DriverId.ToString(),
                    changes: new { Email = email, Role = "Driver" },
                    metadata: new
                    {
                        IpAddress = loginIp,
                        driver.FailedLoginAttempts,
                        driver.LockoutEndUtc
                    });
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

            if (driver.LastLoginAt.HasValue
                && !string.IsNullOrWhiteSpace(driver.LastLoginIp)
                && !string.Equals(driver.LastLoginIp, loginIp, StringComparison.OrdinalIgnoreCase))
            {
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginRiskNewIp",
                    description: $"Driver {driver.Username} signed in from a new IP.",
                    entityType: "Driver",
                    entityId: driver.DriverId.ToString(),
                    metadata: new
                    {
                        PreviousIp = driver.LastLoginIp,
                        CurrentIp = loginIp,
                        PreviousLoginAt = driver.LastLoginAt
                    });
            }

            driver.LastLoginAt = DateTime.UtcNow;
            driver.LastLoginIp = loginIp;
            driver.FailedLoginAttempts = 0;
            driver.LockoutEndUtc = null;
            await _context.SaveChangesAsync();
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginSucceeded",
                description: $"Driver {driver.Username} signed in.",
                entityType: "Driver",
                entityId: driver.DriverId.ToString(),
                metadata: new { driver.Email, IpAddress = loginIp });

            await SignInAsync("Driver", driver.DriverId, driver.Username, driver.Email, userAgent);
            return RedirectToPage(driver.MustResetPassword ? "/ChangePassword" : "/Driver/Dashboard");
        }

        if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == email);

            if (admin == null)
            {
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginFailed",
                    description: $"Failed admin login for {email}.",
                    entityType: "Admin",
                    changes: new { Email = email, Role = "Admin" },
                    metadata: new { IpAddress = loginIp });
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            if (admin.IsSuspended)
            {
                StatusMessage = "This account has been suspended. Contact an administrator.";
                return Page();
            }

            if (admin.LockoutEndUtc.HasValue && admin.LockoutEndUtc.Value > DateTime.UtcNow)
            {
                var localLockoutEnd = DateTime.SpecifyKind(admin.LockoutEndUtc.Value, DateTimeKind.Utc).ToLocalTime();
                StatusMessage = $"Account locked due to failed logins. Try again after {localLockoutEnd:MM/dd/yyyy HH:mm:ss}.";
                return Page();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                admin.LastFailedLoginIp = loginIp;
                admin.FailedLoginAttempts += 1;
                if (admin.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    admin.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                }

                await _context.SaveChangesAsync();
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginFailed",
                    description: $"Failed admin login for {email}.",
                    entityType: "Admin",
                    entityId: admin.AdminId.ToString(),
                    changes: new { Email = email, Role = "Admin" },
                    metadata: new
                    {
                        IpAddress = loginIp,
                        admin.FailedLoginAttempts,
                        admin.LockoutEndUtc
                    });
                StatusMessage = "Invalid email or password.";
                return Page();
            }

            var displayName = string.IsNullOrWhiteSpace(admin.DisplayName) ? "Admin" : admin.DisplayName;
            if (admin.LastLoginAt.HasValue
                && !string.IsNullOrWhiteSpace(admin.LastLoginIp)
                && !string.Equals(admin.LastLoginIp, loginIp, StringComparison.OrdinalIgnoreCase))
            {
                await _auditService.LogEventAsync(
                    category: "Authentication",
                    action: "LoginRiskNewIp",
                    description: $"Admin {displayName} signed in from a new IP.",
                    entityType: "Admin",
                    entityId: admin.AdminId.ToString(),
                    metadata: new
                    {
                        PreviousIp = admin.LastLoginIp,
                        CurrentIp = loginIp,
                        PreviousLoginAt = admin.LastLoginAt
                    });
            }

            admin.LastLoginAt = DateTime.UtcNow;
            admin.LastLoginIp = loginIp;
            admin.FailedLoginAttempts = 0;
            admin.LockoutEndUtc = null;
            await _context.SaveChangesAsync();
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginSucceeded",
                description: $"Admin {displayName} signed in.",
                entityType: "Admin",
                entityId: admin.AdminId.ToString(),
                metadata: new { admin.Email, IpAddress = loginIp });

            await SignInAsync("Admin", admin.AdminId, displayName, admin.Email, userAgent);
            return RedirectToPage(admin.MustResetPassword ? "/ChangePassword" : "/Admin/Dashboard");
        }

        var sponsor = await _context.Sponsors
            .FirstOrDefaultAsync(s => s.Email == email);

        if (sponsor == null)
        {
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginFailed",
                description: $"Failed sponsor login for {email}.",
                entityType: "Sponsor",
                changes: new { Email = email, Role = "Sponsor" },
                metadata: new { IpAddress = loginIp });
            StatusMessage = "Invalid email or password.";
            return Page();
        }

        if (sponsor.IsSuspended)
        {
            StatusMessage = "This account has been suspended. Contact an administrator.";
            return Page();
        }

        if (sponsor.LockoutEndUtc.HasValue && sponsor.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var localLockoutEnd = DateTime.SpecifyKind(sponsor.LockoutEndUtc.Value, DateTimeKind.Utc).ToLocalTime();
            StatusMessage = $"Account locked due to failed logins. Try again after {localLockoutEnd:MM/dd/yyyy HH:mm:ss}.";
            return Page();
        }

        if (!BCrypt.Net.BCrypt.Verify(password, sponsor.PasswordHash))
        {
            sponsor.LastFailedLoginIp = loginIp;
            sponsor.FailedLoginAttempts += 1;
            if (sponsor.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                sponsor.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
            }

            await _context.SaveChangesAsync();
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginFailed",
                description: $"Failed sponsor login for {email}.",
                entityType: "Sponsor",
                entityId: sponsor.SponsorId.ToString(),
                changes: new { Email = email, Role = "Sponsor" },
                metadata: new
                {
                    IpAddress = loginIp,
                    sponsor.FailedLoginAttempts,
                    sponsor.LockoutEndUtc
                });
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

        if (sponsor.LastLoginAt.HasValue
            && !string.IsNullOrWhiteSpace(sponsor.LastLoginIp)
            && !string.Equals(sponsor.LastLoginIp, loginIp, StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.LogEventAsync(
                category: "Authentication",
                action: "LoginRiskNewIp",
                description: $"Sponsor {sponsor.Name} signed in from a new IP.",
                entityType: "Sponsor",
                entityId: sponsor.SponsorId.ToString(),
                metadata: new
                {
                    PreviousIp = sponsor.LastLoginIp,
                    CurrentIp = loginIp,
                    PreviousLoginAt = sponsor.LastLoginAt
                });
        }

        sponsor.LastLoginAt = DateTime.UtcNow;
        sponsor.LastLoginIp = loginIp;
        sponsor.FailedLoginAttempts = 0;
        sponsor.LockoutEndUtc = null;
        await _context.SaveChangesAsync();
        await _auditService.LogEventAsync(
            category: "Authentication",
            action: "LoginSucceeded",
            description: $"Sponsor {sponsor.Name} signed in.",
            entityType: "Sponsor",
            entityId: sponsor.SponsorId.ToString(),
            metadata: new { sponsor.Email, IpAddress = loginIp });

        await SignInAsync("Sponsor", sponsor.SponsorId, sponsor.Name, sponsor.Email, userAgent);
        return RedirectToPage(sponsor.MustResetPassword ? "/ChangePassword" : "/Sponsor/ManagePoints");
    }

    private async Task SignInAsync(string role, int userId, string displayName, string email, string? userAgent)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTime.UtcNow.AddHours(1);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _sessionService.CreateSessionAsync(
            role: role,
            userId: userId,
            sessionId: sessionId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            expiresAtUtc: expiresAtUtc);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, $"{role}:{userId}"),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("sid", sessionId)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = RememberMe,
            ExpiresUtc = expiresAtUtc
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        _logger.LogInformation("{Role} signed in with email {Email}.", role, email);
    }
}
