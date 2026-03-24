using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver
{
    [Authorize(Roles = "Driver")]
    public class CatalogModel : PageModel
    {
        private const int PageSize = 12;
        private readonly ApplicationDbContext _context;
        private readonly ProductCatalogService _productCatalogService;

        public CatalogModel(ApplicationDbContext context, ProductCatalogService productCatalogService)
        {
            _context = context;
            _productCatalogService = productCatalogService;
        }

        public List<Product> Products { get; set; } = new List<Product>();
        public string SponsorName { get; private set; } = string.Empty;
        public int PageNumber { get; private set; } = 1;
        public int TotalPages { get; private set; }
        public int TotalProducts { get; private set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public async Task<IActionResult> OnGetAsync()
        {
            var driver = await LoadDriverAsync();
            if (driver is null)
            {
                return Challenge();
            }

            SponsorName = driver.Sponsor;
            PageNumber = CurrentPage < 1 ? 1 : CurrentPage;

            var sponsor = await _context.Sponsors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == driver.Sponsor);

            if (sponsor is null)
            {
                Products = new List<Product>();
                return Page();
            }

            var selectedProductIds = await _context.SponsorCatalogProducts.AsNoTracking()
                .Where(scp => scp.SponsorId == sponsor.SponsorId)
                .Select(scp => scp.ProductId)
                .ToHashSetAsync();

            if (selectedProductIds.Count == 0)
            {
                Products = new List<Product>();
                TotalPages = 0;
                TotalProducts = 0;
                return Page();
            }

            var allProducts = await _productCatalogService.GetAllProductsAsync();

            var query = allProducts
                .Where(p => selectedProductIds.Contains(p.Id));

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(p =>
                    p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            var filteredProducts = query
                .OrderBy(p => p.Name)
                .ToList();

            TotalProducts = filteredProducts.Count;
            TotalPages = TotalProducts == 0 ? 0 : (int)Math.Ceiling(TotalProducts / (double)PageSize);

            if (TotalPages > 0 && PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Products = filteredProducts
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
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
}
