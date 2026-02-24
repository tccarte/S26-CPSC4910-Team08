using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DriverRewards.Models;

namespace DriverRewards.Pages.Driver
{
    public class CatalogModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public CatalogModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public List<Product> Products { get; set; } = new List<Product>();

        public async Task OnGetAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://fakestoreapi.com/products");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    
                    Products = JsonSerializer.Deserialize<List<Product>>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Product>();
                }
            }
            catch
            {
                Products = new List<Product>();
            }
        }
    }
}
