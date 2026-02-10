using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_sponsor_change_requests")]
public class SponsorChangeRequest
{
    [Key]
    [Column("request_id")]
    public int RequestId { get; set; }

    [Required]
    [Column("driver_id")]
    public int DriverId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("current_sponsor")]
    public string CurrentSponsor { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("requested_sponsor")]
    public string RequestedSponsor { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Required]
    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
