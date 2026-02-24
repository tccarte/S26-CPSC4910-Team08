using System.Text.Json.Serialization;

namespace DriverRewards.Models
{
    public class Product
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } 

        [JsonPropertyName("title")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("price")]
        public decimal PriceInPoints { get; set; } 

        [JsonPropertyName("image")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }
    }
}