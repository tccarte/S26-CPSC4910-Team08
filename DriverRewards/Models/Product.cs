using System.Text.Json.Serialization;

namespace DriverRewards.Models
{
    public class Product
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } 

        [JsonPropertyName("title")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal PriceInPoints { get; set; } 

        [JsonPropertyName("image")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public ProductRating? Rating { get; set; }
    }

    public class ProductRating
    {
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
