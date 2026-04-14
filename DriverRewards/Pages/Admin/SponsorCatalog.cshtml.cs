using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SponsorCatalogModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ProductCatalogService _productCatalogService;

    public SponsorCatalogModel(ApplicationDbContext context, ProductCatalogService productCatalogService)
    {
        _context = context;
        _productCatalogService = productCatalogService;
    }

    public Models.Sponsor? Sponsor { get; private set; }
    public List<Product> Products { get; private set; } = new();
    public int DriverCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? SponsorId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Sponsor = await ResolveSponsorAsync();
        if (Sponsor is null)
        {
            return NotFound();
        }

        DriverCount = await _context.Drivers.AsNoTracking()
            .CountAsync(d => d.Sponsor == Sponsor.Name);

        var selectedProductIds = await _context.SponsorCatalogProducts.AsNoTracking()
            .Where(scp => scp.SponsorId == Sponsor.SponsorId)
            .Select(scp => scp.ProductId)
            .ToHashSetAsync();

        if (selectedProductIds.Count == 0)
        {
            return Page();
        }

        var allProducts = await _productCatalogService.GetAllProductsAsync();
        Products = allProducts
            .Where(p => selectedProductIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToList();

        return Page();
    }

    private async Task<Models.Sponsor?> ResolveSponsorAsync()
    {
        if (SponsorId.HasValue && SponsorId.Value > 0)
        {
            return await _context.Sponsors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SponsorId == SponsorId.Value);
        }

        if (Id.HasValue && Id.Value > 0)
        {
            var driver = await _context.Drivers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DriverId == Id.Value);
            if (driver is null)
            {
                return null;
            }

            return await _context.Sponsors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == driver.Sponsor);
        }

        return null;
    }
}
