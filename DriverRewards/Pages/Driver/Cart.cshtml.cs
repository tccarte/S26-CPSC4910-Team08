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
public class CartModel : PageModel
{
    private const string CartSessionKey = "DriverCart";
    private readonly ApplicationDbContext _context;

    public CartModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CartItem> Items { get; private set; } = new();
    public int CurrentPoints { get; private set; }

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

    public IActionResult OnPostRemove(int productId)
    {
        LoadCart();
        Items.RemoveAll(i => i.ProductId == productId);
        HttpContext.Session.SetJson(CartSessionKey, Items);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        HttpContext.Session.Remove(CartSessionKey);
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
