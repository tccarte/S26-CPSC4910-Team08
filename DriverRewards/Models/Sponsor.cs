using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_sponsors")]
public class Sponsor
{
    [Key]
    [Column("sponsor_id")]
    public int SponsorId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [StringLength(20)]
    [Column("support_phone")]
    public string? SupportPhone { get; set; }

    [StringLength(255)]
    [Column("headquarters_address")]
    public string? HeadquartersAddress { get; set; }

    [Required]
    [Column("dollar_to_point_ratio", TypeName = "decimal(10,2)")]
    public decimal DollarToPointRatio { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
