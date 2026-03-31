using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class ManageCatalogModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ProductCatalogService _productCatalogService;
    private readonly AuditService _auditService;

    public ManageCatalogModel(ApplicationDbContext context, ProductCatalogService productCatalogService, AuditService auditService)
    {
        _context = context;
        _productCatalogService = productCatalogService;
        _auditService = auditService;
    }

    public string SponsorName { get; private set; } = string.Empty;
    public List<Product> Products { get; private set; } = new();
    public HashSet<int> SavedProductIds { get; private set; } = new();

    [BindProperty]
    public List<int> SelectedProductIds { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sponsor = await LoadSponsorAsync();
        if (sponsor is null)
        {
            return Challenge();
        }

        await LoadPageStateAsync(sponsor);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var sponsor = await LoadSponsorAsync();
        if (sponsor is null)
        {
            return Challenge();
        }

        var existingSelections = await _context.SponsorCatalogProducts
            .Where(scp => scp.SponsorId == sponsor.SponsorId)
            .ToListAsync();

        _context.SponsorCatalogProducts.RemoveRange(existingSelections);

        var distinctSelectedIds = SelectedProductIds
            .Distinct()
            .ToList();

        foreach (var productId in distinctSelectedIds)
        {
            _context.SponsorCatalogProducts.Add(new SponsorCatalogProduct
            {
                SponsorId = sponsor.SponsorId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await _auditService.LogEventAsync(
            category: "Catalog",
            action: "CatalogPublished",
            description: $"{sponsor.Name} saved {distinctSelectedIds.Count} catalog product(s).",
            entityType: "Sponsor",
            entityId: sponsor.SponsorId.ToString(),
            changes: new
            {
                PreviousProductIds = existingSelections.Select(s => s.ProductId).OrderBy(id => id).ToList(),
                NewProductIds = distinctSelectedIds.OrderBy(id => id).ToList()
            });

        TempData["StatusMessage"] = distinctSelectedIds.Count == 0
            ? "Your driver catalog is now empty."
            : $"Saved {distinctSelectedIds.Count} product(s) to your driver catalog.";

        return RedirectToPage();
    }

    private async Task LoadPageStateAsync(DriverRewards.Models.Sponsor sponsor)
    {
        SponsorName = sponsor.Name;

        SavedProductIds = await _context.SponsorCatalogProducts.AsNoTracking()
            .Where(scp => scp.SponsorId == sponsor.SponsorId)
            .Select(scp => scp.ProductId)
            .ToHashSetAsync();

        SelectedProductIds = SavedProductIds.ToList();

        var allProducts = await _productCatalogService.GetAllProductsAsync();
        Products = allProducts
            .OrderBy(p => p.Name)
            .ToList();
    }

    private async Task<DriverRewards.Models.Sponsor?> LoadSponsorAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return null;
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Sponsor", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!int.TryParse(parts[1], out var sponsorId))
        {
            return null;
        }

        return await _context.Sponsors.FirstOrDefaultAsync(s => s.SponsorId == sponsorId);
    }
}
