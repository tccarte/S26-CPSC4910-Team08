using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverEntity = DriverRewards.Models.Driver;
using DriverRewards.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages;

[Authorize(Roles = "Driver")]
public class ProfileModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ProfileModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ProfileInput Profile { get; set; } = new();

    [BindProperty]
    public SponsorRequestInput SponsorRequest { get; set; } = new();

    public string CurrentSponsor { get; private set; } = string.Empty;
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var driver = await LoadDriverAsync();
        if (driver == null)
        {
            return NotFound();
        }

        Profile.Email = driver.Email;
        Profile.Username = driver.Username;
        Profile.FedexId = driver.FedexId;
        CurrentSponsor = driver.Sponsor;
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync()
    {
        var driver = await LoadDriverAsync();
        if (driver == null)
        {
            return NotFound();
        }

        CurrentSponsor = driver.Sponsor;

        if (!TryValidateModel(Profile, nameof(Profile)))
        {
            return Page();
        }

        var usernameTaken = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Username == Profile.Username && d.DriverId != driver.DriverId);
        if (usernameTaken)
        {
            ModelState.AddModelError("Profile.Username", "Username is already taken.");
            return Page();
        }

        var emailTaken = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Email == Profile.Email && d.DriverId != driver.DriverId);
        if (emailTaken)
        {
            ModelState.AddModelError("Profile.Email", "Email is already registered.");
            return Page();
        }

        driver.Username = Profile.Username.Trim();
        driver.Email = Profile.Email.Trim();
        driver.FedexId = string.IsNullOrWhiteSpace(Profile.FedexId) ? null : Profile.FedexId.Trim();

        await _context.SaveChangesAsync();

        StatusMessage = "Profile updated successfully.";
        return Page();
    }

    public async Task<IActionResult> OnPostSponsorAsync()
    {
        var driver = await LoadDriverAsync();
        if (driver == null)
        {
            return NotFound();
        }

        Profile.Email = driver.Email;
        Profile.Username = driver.Username;
        Profile.FedexId = driver.FedexId;
        CurrentSponsor = driver.Sponsor;

        if (!TryValidateModel(SponsorRequest, nameof(SponsorRequest)))
        {
            return Page();
        }

        var request = new SponsorChangeRequest
        {
            DriverId = driver.DriverId,
            CurrentSponsor = driver.Sponsor,
            RequestedSponsor = SponsorRequest.RequestedSponsor.Trim(),
            Note = string.IsNullOrWhiteSpace(SponsorRequest.Note) ? null : SponsorRequest.Note.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.SponsorChangeRequests.Add(request);
        await _context.SaveChangesAsync();

        StatusMessage = "Sponsor change request submitted.";
        SponsorRequest = new SponsorRequestInput();
        return Page();
    }

    private async Task<DriverEntity?> LoadDriverAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return null;
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Driver", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!int.TryParse(parts[1], out var driverId))
        {
            return null;
        }

        return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
    }

    public class ProfileInput
    {
        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "FedEx ID")]
        public string? FedexId { get; set; }
    }

    public class SponsorRequestInput
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Requested Sponsor")]
        public string RequestedSponsor { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Reason (optional)")]
        public string? Note { get; set; }
    }
}
