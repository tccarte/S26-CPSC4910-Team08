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
public class CartModel : PageModel
{
    private const string CartSessionKey = "DriverCart";
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public CartModel(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public List<CartItem> Items { get; private set; } = new();
    public int CurrentPoints { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public decimal TotalPoints => Items.Sum(i => i.PriceInPoints * i.Quantity);
    public decimal RemainingPoints => CurrentPoints - TotalPoints;

    public async Task<IActionResult> OnGetAsync()
    {
        LoadCart();
        var result = await LoadCurrentPointsAsync();
        if (result != null)
        {
            return result;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRemove(int productId)
    {
        LoadCart();
        var removedItem = Items.FirstOrDefault(i => i.ProductId == productId);
        Items.RemoveAll(i => i.ProductId == productId);
        HttpContext.Session.SetJson(CartSessionKey, Items);
        if (removedItem != null)
        {
            await _auditService.LogEventAsync(
                category: "Cart",
                action: "RemoveItem",
                description: $"Removed {removedItem.Name} from cart.",
                entityType: "Product",
                entityId: removedItem.ProductId.ToString(),
                metadata: new { removedItem.Name, removedItem.Quantity, removedItem.PriceInPoints });
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClear()
    {
        LoadCart();
        HttpContext.Session.Remove(CartSessionKey);
        await _auditService.LogEventAsync(
            category: "Cart",
            action: "Clear",
            description: $"Cleared cart with {Items.Count} item(s).",
            metadata: new { ItemCount = Items.Count, TotalPoints });
        return RedirectToPage();
    }

    private void LoadCart()
    {
        Items = HttpContext.Session.GetJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
    }

    private async Task<IActionResult?> LoadCurrentPointsAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Challenge();
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Driver", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!int.TryParse(parts[1], out var driverId))
        {
            return Forbid();
        }

        var driver = await _context.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DriverId == driverId);

        if (driver == null)
        {
            return NotFound();
        }

        CurrentPoints = driver.NumPoints ?? 0;
        return null;
    }
}
