using System.Text.Json;
using DriverRewards.Models;

namespace DriverRewards.Services;

public class ProductCatalogService
{
    private readonly HttpClient _httpClient;

    public ProductCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://dummyjson.com/products?limit=100");
            if (!response.IsSuccessStatusCode)
            {
                return new List<Product>();
            }

        var jsonString = await response.Content.ReadAsStringAsync();        
        var result = JsonSerializer.Deserialize<DummyJsonResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Products ?? new List<Product>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Error: {ex.Message}");
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://dummyjson.com/products/{id}");
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
