using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_driver_sponsors")]
public class DriverSponsor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("driver_id")]
    public int DriverId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("sponsor_name")]
    public string SponsorName { get; set; } = string.Empty;

    [Column("is_approved")]
    public bool IsApproved { get; set; } = false;

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Driver Driver { get; set; } = null!;
}
