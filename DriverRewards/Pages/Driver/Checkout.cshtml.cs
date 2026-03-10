using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Extensions;
using DriverRewards.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver;

[Authorize(Roles = "Driver")]
public class CheckoutModel : PageModel
{
    private const string CartSessionKey = "DriverCart";
    private readonly ApplicationDbContext _context;

    public CheckoutModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CartItem> Items { get; private set; } = new();
    public int CurrentPoints { get; private set; }
    public decimal TotalPoints => Items.Sum(i => i.PriceInPoints * i.Quantity);
    public int PointsToCharge => (int)Math.Ceiling(TotalPoints);

    [BindProperty]
    public ShippingInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        LoadCart();
        if (!Items.Any())
        {
            StatusMessage = "Your cart is empty.";
            return RedirectToPage("/Driver/Cart");
        }

        var result = await LoadCurrentPointsAsync();
        if (result != null)
        {
            return result;
        }

        var driver = await GetCurrentDriverAsync();
        if (driver != null)
        {
            Input.FullName = driver.ShippingFullName ?? $"{driver.FirstName} {driver.LastName}".Trim();
            Input.AddressLine1 = driver.ShippingAddressLine1 ?? string.Empty;
            Input.AddressLine2 = driver.ShippingAddressLine2;
            Input.City = driver.ShippingCity ?? string.Empty;
            Input.State = driver.ShippingState ?? string.Empty;
            Input.PostalCode = driver.ShippingPostalCode ?? string.Empty;
            Input.Country = driver.ShippingCountry ?? "United States";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadCart();
        if (!Items.Any())
        {
            StatusMessage = "Your cart is empty.";
            return RedirectToPage("/Driver/Cart");
        }

        var driver = await GetCurrentDriverAsync();
        if (driver == null)
        {
            return Challenge();
        }

        CurrentPoints = driver.NumPoints ?? 0;
        if (CurrentPoints < PointsToCharge)
        {
            ModelState.AddModelError(string.Empty, "You do not have enough points to complete checkout.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        driver.ShippingFullName = Input.FullName.Trim();
        driver.ShippingAddressLine1 = Input.AddressLine1.Trim();
        driver.ShippingAddressLine2 = string.IsNullOrWhiteSpace(Input.AddressLine2) ? null : Input.AddressLine2.Trim();
        driver.ShippingCity = Input.City.Trim();
        driver.ShippingState = Input.State.Trim();
        driver.ShippingPostalCode = Input.PostalCode.Trim();
        driver.ShippingCountry = Input.Country.Trim();
        driver.NumPoints = CurrentPoints - PointsToCharge;
        await _context.SaveChangesAsync();

        HttpContext.Session.Remove(CartSessionKey);
        StatusMessage = $"Order placed successfully for {PointsToCharge} points. Shipping to {Input.FullName}.";

        return RedirectToPage("/Driver/Cart");
    }

    private void LoadCart()
    {
        Items = HttpContext.Session.GetJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
    }

    private async Task<IActionResult?> LoadCurrentPointsAsync()
    {
        var driver = await GetCurrentDriverAsync();
        if (driver == null)
        {
            return Challenge();
        }

        CurrentPoints = driver.NumPoints ?? 0;
        return null;
    }

    private async Task<Models.Driver?> GetCurrentDriverAsync()
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

    public class ShippingInput
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(80)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        [Display(Name = "State/Province")]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Country { get; set; } = "United States";
    }
}
