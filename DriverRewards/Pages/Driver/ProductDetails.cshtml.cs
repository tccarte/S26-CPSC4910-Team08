using System.Net.Http;
using System.Text.Json;
using DriverRewards.Extensions;
using DriverRewards.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DriverRewards.Pages.Driver;

public class ProductDetailsModel : PageModel
{
    private const string CartSessionKey = "DriverCart";
    private readonly HttpClient _httpClient;

    public ProductDetailsModel(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Product? Product { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Product = await GetProductAsync(id);
        if (Product is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id)
    {
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
        try
        {
            var response = await _httpClient.GetAsync($"https://fakestoreapi.com/products/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Product>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
