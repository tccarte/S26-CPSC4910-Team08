using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DriverRewards.Data;
using DriverRewards.Models;
using System.ComponentModel.DataAnnotations;

namespace DriverRewards.Pages.Admin;

public class CreateSponsorModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateSponsorModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Organization Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Admin Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Initial Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Point Ratio ($ to Points)")]
        [Range(0.01, 100.00)]
        public decimal DollarToPointRatio { get; set; } = 0.01m;
        
        [Phone]
        [Display(Name = "Main Phone")]
        public string? Phone { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Ensure email is unique across sponsors
        if (_context.Sponsors.Any(s => s.Email == Input.Email))
        {
            ModelState.AddModelError("Input.Email", "A sponsor with this email already exists.");
            return Page();
        }

        var sponsor = new Sponsor
        {
            Name = Input.Name,
            Email = Input.Email,
            // Securely hash the password before saving
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password), 
            DollarToPointRatio = Input.DollarToPointRatio,
            Phone = Input.Phone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sponsors.Add(sponsor);
        await _context.SaveChangesAsync();

        StatusMessage = $"Sponsor '{Input.Name}' created successfully.";
        return RedirectToPage("./ManageSponsors");
    }
}