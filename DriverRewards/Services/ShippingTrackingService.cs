using System.Globalization;
using System.Text.Json;
using DriverRewards.Models;

namespace DriverRewards.Services;

public class ShippingTrackingService
{
    private const double WarehouseLatitude = 35.042036;
    private const double WarehouseLongitude = -89.976227;
    private const double TransitSpeedMilesPerHour = 420;
    private readonly HttpClient _httpClient;

    public ShippingTrackingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DriverRewards/1.0");
        }
    }

    public decimal WarehouseLatitudeValue => (decimal)WarehouseLatitude;
    public decimal WarehouseLongitudeValue => (decimal)WarehouseLongitude;

    public async Task<GeocodingResult?> GeocodeAsync(string address)
    {
        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var response = await _httpClient.GetAsync($"https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q={encodedAddress}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var match = results?.FirstOrDefault();
            if (match is null
                || !decimal.TryParse(match.Lat, NumberStyles.Any, CultureInfo.InvariantCulture, out var latitude)
                || !decimal.TryParse(match.Lon, NumberStyles.Any, CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            return new GeocodingResult
            {
                Latitude = latitude,
                Longitude = longitude,
                DisplayName = match.DisplayName ?? string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    public decimal CalculateDistanceMiles(decimal destinationLatitude, decimal destinationLongitude)
    {
        return Math.Round((decimal)CalculateDistanceMilesInternal(
            WarehouseLatitude,
            WarehouseLongitude,
            (double)destinationLatitude,
            (double)destinationLongitude), 1);
    }

    public DateTime EstimateDelivery(DateTime placedAtUtc, decimal distanceMiles)
    {
        var miles = Math.Max((double)distanceMiles, 80d);
        var estimatedHours = Math.Max(24d, miles / TransitSpeedMilesPerHour * 24d);
        var wholeDays = (int)Math.Ceiling(estimatedHours / 24d);
        return placedAtUtc.AddDays(Math.Clamp(wholeDays, 2, 7));
    }

    public string BuildTrackingNumber(int driverId)
    {
        return $"DR-{DateTime.UtcNow:yyyyMMdd}-{driverId:D4}-{Random.Shared.Next(1000, 9999)}";
    }

    public string BuildAddressLine(string line1, string? line2, string city, string state, string postalCode, string country)
    {
        return string.Join(", ", new[]
        {
            line1,
            string.IsNullOrWhiteSpace(line2) ? null : line2,
            city,
            state,
            postalCode,
            country
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static double CalculateDistanceMilesInternal(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMiles = 3958.8;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMiles * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private sealed class NominatimResult
    {
        public string? Lat { get; set; }
        public string? Lon { get; set; }
        public string? DisplayName { get; set; }
    }
}
