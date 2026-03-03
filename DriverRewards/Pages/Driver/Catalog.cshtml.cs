using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://fakestoreapi.com/products");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    
                    var allProducts = JsonSerializer.Deserialize<List<Product>>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Product>();

                    var query = allProducts.AsQueryable();

                    if (!string.IsNullOrEmpty(SearchQuery))
                    {
                        query = query.Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                                 p.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                    }

                    Products = query.ToList();
                }
            }
            catch
            {
                Products = new List<Product>();
            }
        }
    }
}
