using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Pages.Driver;

[Authorize(Roles = "Driver")]
public class OrdersModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ShippingTrackingService _shippingTrackingService;

    public OrdersModel(ApplicationDbContext context, ShippingTrackingService shippingTrackingService)
    {
        _context = context;
        _shippingTrackingService = shippingTrackingService;
    }

    public List<OrderCard> Orders { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var driverId = GetCurrentDriverId();
        if (driverId is null)
        {
            return Challenge();
        }

        var orders = await _context.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.DriverId == driverId.Value)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        Orders = orders.Select(BuildOrderCard).ToList();
        return Page();
    }

    private OrderCard BuildOrderCard(Order order)
    {
        var progress = CalculateProgress(order.PlacedAt, order.EstimatedDeliveryAt, DateTime.UtcNow);
        var status = ResolveStatus(progress, order.EstimatedDeliveryAt);
        var currentCoordinates = InterpolateLocation(progress, order.DestinationLatitude, order.DestinationLongitude);

        return new OrderCard
        {
            OrderId = order.OrderId,
            TrackingNumber = order.TrackingNumber,
            Status = status,
            ProgressPercent = (int)Math.Round(progress * 100m),
            EstimatedDistanceMiles = order.EstimatedDistanceMiles,
            PlacedAt = order.PlacedAt,
            EstimatedDeliveryAt = order.EstimatedDeliveryAt,
            ShippingLabel = $"{order.ShippingCity}, {order.ShippingState} {order.ShippingPostalCode}",
            ShippingAddress = $"{order.ShippingAddressLine1}, {order.ShippingCity}, {order.ShippingState} {order.ShippingPostalCode}, {order.ShippingCountry}",
            TotalPoints = order.TotalPoints,
            WarehouseLatitude = _shippingTrackingService.WarehouseLatitudeValue,
            WarehouseLongitude = _shippingTrackingService.WarehouseLongitudeValue,
            DestinationLatitude = order.DestinationLatitude,
            DestinationLongitude = order.DestinationLongitude,
            CurrentLatitude = currentCoordinates.latitude,
            CurrentLongitude = currentCoordinates.longitude,
            CanRenderMap = order.DestinationLatitude.HasValue && order.DestinationLongitude.HasValue,
            Timeline = BuildTimeline(progress, order.EstimatedDeliveryAt),
            Items = order.Items
                .OrderBy(i => i.ProductName)
                .Select(i => new OrderItemRow
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    PriceInPoints = i.PriceInPoints
                })
                .ToList()
        };
    }

    private static decimal CalculateProgress(DateTime placedAt, DateTime estimatedDeliveryAt, DateTime nowUtc)
    {
        if (estimatedDeliveryAt <= placedAt)
        {
            return 1m;
        }

        var total = (decimal)(estimatedDeliveryAt - placedAt).TotalMinutes;
        var elapsed = (decimal)(nowUtc - placedAt).TotalMinutes;
        var progress = total <= 0m ? 1m : elapsed / total;
        return Math.Clamp(progress, 0m, 1m);
    }

    private static string ResolveStatus(decimal progress, DateTime estimatedDeliveryAt)
    {
        if (DateTime.UtcNow >= estimatedDeliveryAt)
        {
            return "Delivered";
        }

        if (progress < 0.15m) return "Order received";
        if (progress < 0.35m) return "Processing at warehouse";
        if (progress < 0.80m) return "In transit";
        return "Out for delivery";
    }

    private (decimal? latitude, decimal? longitude) InterpolateLocation(decimal progress, decimal? destinationLatitude, decimal? destinationLongitude)
    {
        if (!destinationLatitude.HasValue || !destinationLongitude.HasValue)
        {
            return (null, null);
        }

        var easedProgress = 1m - (decimal)Math.Pow((double)(1m - progress), 1.4);
        return (
            _shippingTrackingService.WarehouseLatitudeValue + ((destinationLatitude.Value - _shippingTrackingService.WarehouseLatitudeValue) * easedProgress),
            _shippingTrackingService.WarehouseLongitudeValue + ((destinationLongitude.Value - _shippingTrackingService.WarehouseLongitudeValue) * easedProgress));
    }

    private static List<TimelineStep> BuildTimeline(decimal progress, DateTime estimatedDeliveryAt)
    {
        var steps = new List<TimelineStep>
        {
            new() { Label = "Order received", IsComplete = progress >= 0.05m },
            new() { Label = "Packed at warehouse", IsComplete = progress >= 0.25m },
            new() { Label = "In transit", IsComplete = progress >= 0.55m },
            new() { Label = "Out for delivery", IsComplete = progress >= 0.85m },
            new() { Label = "Delivered", IsComplete = DateTime.UtcNow >= estimatedDeliveryAt }
        };

        var activeIndex = steps.FindIndex(step => !step.IsComplete);
        if (activeIndex >= 0)
        {
            steps[activeIndex].IsCurrent = true;
        }
        else if (steps.Count > 0)
        {
            steps[^1].IsCurrent = true;
        }

        return steps;
    }

    private int? GetCurrentDriverId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return null;
        }

        var parts = userIdClaim.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Driver", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(parts[1], out var driverId) ? driverId : null;
    }

    public class OrderCard
    {
        public int OrderId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public decimal EstimatedDistanceMiles { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime EstimatedDeliveryAt { get; set; }
        public string ShippingLabel { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public decimal WarehouseLatitude { get; set; }
        public decimal WarehouseLongitude { get; set; }
        public decimal? DestinationLatitude { get; set; }
        public decimal? DestinationLongitude { get; set; }
        public decimal? CurrentLatitude { get; set; }
        public decimal? CurrentLongitude { get; set; }
        public bool CanRenderMap { get; set; }
        public List<TimelineStep> Timeline { get; set; } = new();
        public List<OrderItemRow> Items { get; set; } = new();
    }

    public class TimelineStep
    {
        public string Label { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class OrderItemRow
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PriceInPoints { get; set; }
    }
}
