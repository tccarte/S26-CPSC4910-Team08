using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_driver_notifications")]
public class DriverNotification
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Column("driver_id")]
    public int DriverId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    public Driver Driver { get; set; } = null!;
}
