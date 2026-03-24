using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Extensions;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver;

[Authorize(Roles = "Driver")]
public class ProductDetailsModel : PageModel
{
    private const string CartSessionKey = "DriverCart";
    private readonly ApplicationDbContext _context;
    private readonly ProductCatalogService _productCatalogService;

    public ProductDetailsModel(ApplicationDbContext context, ProductCatalogService productCatalogService)
    {
        _context = context;
        _productCatalogService = productCatalogService;
    }

    public Product? Product { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await DriverCanAccessProductAsync(id))
        {
            return NotFound();
        }

        Product = await GetProductAsync(id);
        if (Product is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id)
    {
        if (!await DriverCanAccessProductAsync(id))
        {
            return NotFound();
        }

        var product = await GetProductAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var cart = HttpContext.Session.GetJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        var existingItem = cart.FirstOrDefault(c => c.ProductId == product.Id);

        if (existingItem is null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Category = product.Category,
                PriceInPoints = product.PriceInPoints,
                Quantity = 1
            });
        }
        else
        {
            existingItem.Quantity++;
        }

        HttpContext.Session.SetJson(CartSessionKey, cart);
        StatusMessage = $"{product.Name} added to cart.";

        return RedirectToPage(new { id });
    }

    private async Task<Product?> GetProductAsync(int id)
    {
        return await _productCatalogService.GetProductAsync(id);
    }

    private async Task<bool> DriverCanAccessProductAsync(int productId)
    {
        var driver = await LoadDriverAsync();
        if (driver is null)
        {
            return false;
        }

        var sponsorId = await _context.Sponsors.AsNoTracking()
            .Where(s => s.Name == driver.Sponsor)
            .Select(s => (int?)s.SponsorId)
            .FirstOrDefaultAsync();

        if (!sponsorId.HasValue)
        {
            return false;
        }

        return await _context.SponsorCatalogProducts.AsNoTracking()
            .AnyAsync(scp => scp.SponsorId == sponsorId.Value && scp.ProductId == productId);
    }

    private async Task<Models.Driver?> LoadDriverAsync()
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

        return await _context.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DriverId == driverId);
    }
}
