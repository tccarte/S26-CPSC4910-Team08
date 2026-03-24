using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_orders")]
public class Order
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("driver_id")]
    public int DriverId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("shipping_full_name")]
    public string ShippingFullName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Column("shipping_address_line1")]
    public string ShippingAddressLine1 { get; set; } = string.Empty;

    [StringLength(120)]
    [Column("shipping_address_line2")]
    public string? ShippingAddressLine2 { get; set; }

    [Required]
    [StringLength(80)]
    [Column("shipping_city")]
    public string ShippingCity { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Column("shipping_state")]
    public string ShippingState { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Column("shipping_postal_code")]
    public string ShippingPostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Column("shipping_country")]
    public string ShippingCountry { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("total_points")]
    public int TotalPoints { get; set; }

    [Column("estimated_distance_miles", TypeName = "decimal(10,2)")]
    public decimal EstimatedDistanceMiles { get; set; }

    [Column("destination_latitude", TypeName = "decimal(10,6)")]
    public decimal? DestinationLatitude { get; set; }

    [Column("destination_longitude", TypeName = "decimal(10,6)")]
    public decimal? DestinationLongitude { get; set; }

    [Column("placed_at")]
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    [Column("estimated_delivery_at")]
    public DateTime EstimatedDeliveryAt { get; set; }

    public Driver? Driver { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
