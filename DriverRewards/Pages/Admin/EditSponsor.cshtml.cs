using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditSponsorModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditSponsorModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public int SponsorId { get; set; }

    [BindProperty]
    [Required]
    [StringLength(50)]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [BindProperty]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Support Phone")]
    public string? SupportPhone { get; set; }

    [BindProperty]
    [StringLength(255)]
    [Display(Name = "Headquarters Address")]
    public string? HeadquartersAddress { get; set; }

    [BindProperty]
    [Range(0.01, 100000)]
    [Display(Name = "Dollar-to-Point Ratio")]
    public decimal DollarToPointRatio { get; set; }

    [BindProperty]
    [Display(Name = "Approved")]
    public bool IsApproved { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sponsor = await _context.Sponsors.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SponsorId == id);
        if (sponsor == null)
        {
            return NotFound();
        }

        SponsorId = sponsor.SponsorId;
        Name = sponsor.Name;
        Email = sponsor.Email;
        Phone = sponsor.Phone;
        SupportPhone = sponsor.SupportPhone;
        HeadquartersAddress = sponsor.HeadquartersAddress;
        DollarToPointRatio = sponsor.DollarToPointRatio;
        IsApproved = sponsor.IsApproved;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var sponsor = await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == SponsorId);
        if (sponsor == null)
        {
            return NotFound();
        }

        var emailTaken = await _context.Sponsors.AsNoTracking()
            .AnyAsync(s => s.Email == Email && s.SponsorId != SponsorId);
        if (emailTaken)
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return Page();
        }

        sponsor.Name = Name.Trim();
        sponsor.Email = Email.Trim();
        sponsor.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        sponsor.SupportPhone = string.IsNullOrWhiteSpace(SupportPhone) ? null : SupportPhone.Trim();
        sponsor.HeadquartersAddress = string.IsNullOrWhiteSpace(HeadquartersAddress) ? null : HeadquartersAddress.Trim();
        sponsor.DollarToPointRatio = DollarToPointRatio;
        sponsor.IsApproved = IsApproved;

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Dashboard");
    }
}
