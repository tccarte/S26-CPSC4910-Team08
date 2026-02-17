using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_behaviors")]
public class Behavior
{
    [Key]
    [Column("behavior_id")]
    public int BehaviorId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("points")]
    public int Points { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
