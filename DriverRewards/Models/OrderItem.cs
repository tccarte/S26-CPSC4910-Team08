using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_order_items")]
public class OrderItem
{
    [Key]
    [Column("order_item_id")]
    public int OrderItemId { get; set; }

    [Required]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price_in_points", TypeName = "decimal(10,2)")]
    public decimal PriceInPoints { get; set; }

    public Order? Order { get; set; }
}
