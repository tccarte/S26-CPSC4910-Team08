using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_sponsor_catalog_products")]
public class SponsorCatalogProduct
{
    [Key]
    [Column("sponsor_catalog_product_id")]
    public int SponsorCatalogProductId { get; set; }

    [Required]
    [Column("sponsor_id")]
    public int SponsorId { get; set; }

    [Required]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Sponsor? Sponsor { get; set; }
}
