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

        public List<string> Categories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }
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

            PageNumber = CurrentPage < 1 ? 1 : CurrentPage;

            var sponsorNames = await _context.DriverSponsors.AsNoTracking()
                .Where(ds => ds.DriverId == driver.DriverId && ds.IsApproved)
                .Select(ds => ds.SponsorName)
                .ToListAsync();

            SponsorName = sponsorNames.Count == 1
                ? sponsorNames[0]
                : sponsorNames.Count > 1 ? "Multiple Sponsors" : string.Empty;

            if (sponsorNames.Count == 0)
            {
                Products = new List<Product>();
                return Page();
            }

            var sponsorIds = await _context.Sponsors.AsNoTracking()
                .Where(s => sponsorNames.Contains(s.Name))
                .Select(s => s.SponsorId)
                .ToListAsync();

            if (sponsorIds.Count == 0)
            {
                Products = new List<Product>();
                return Page();
            }

            var selectedProductIds = await _context.SponsorCatalogProducts.AsNoTracking()
                .Where(scp => sponsorIds.Contains(scp.SponsorId))
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
            var sponsorProducts = allProducts.Where(p => selectedProductIds.Contains(p.Id)).ToList();
            
            Categories = sponsorProducts
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var query = sponsorProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(p =>
                    p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(CategoryFilter))
            {
                query = query.Where(p => p.Category.Equals(CategoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            query = SortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.PriceInPoints),
                "price_desc" => query.OrderByDescending(p => p.PriceInPoints),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderBy(p => p.Name), 
            };

            var filteredProducts = query.ToList();

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
