using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditDriverModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditDriverModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public int DriverId { get; set; }

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
    [StringLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(50)]
    [Display(Name = "Sponsor")]
    public string Sponsor { get; set; } = string.Empty;

    [BindProperty]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [BindProperty]
    [StringLength(50)]
    [Display(Name = "FedEx ID")]
    public string? FedexId { get; set; }

    [BindProperty]
    [Range(0, int.MaxValue)]
    [Display(Name = "Points")]
    public int Points { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var driver = await _context.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DriverId == id);
        if (driver == null)
        {
            return NotFound();
        }

        DriverId = driver.DriverId;
        FirstName = driver.FirstName;
        LastName = driver.LastName;
        Username = driver.Username;
        Email = driver.Email;
        Sponsor = driver.Sponsor;
        Phone = driver.Phone;
        FedexId = driver.FedexId;
        Points = driver.NumPoints ?? 0;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == DriverId);
        if (driver == null)
        {
            return NotFound();
        }

        var usernameTaken = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Username == Username && d.DriverId != DriverId);
        if (usernameTaken)
        {
            ModelState.AddModelError("Username", "Username is already taken.");
            return Page();
        }

        var emailTaken = await _context.Drivers.AsNoTracking()
            .AnyAsync(d => d.Email == Email && d.DriverId != DriverId);
        if (emailTaken)
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return Page();
        }

        driver.FirstName = FirstName.Trim();
        driver.LastName = LastName.Trim();
        driver.Username = Username.Trim();
        driver.Email = Email.Trim();
        driver.Sponsor = Sponsor.Trim();
        driver.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        driver.FedexId = string.IsNullOrWhiteSpace(FedexId) ? null : FedexId.Trim();
        driver.NumPoints = Points;

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Dashboard");
    }
}
