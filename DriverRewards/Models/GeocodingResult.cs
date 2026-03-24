namespace DriverRewards.Models;

public class GeocodingResult
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
