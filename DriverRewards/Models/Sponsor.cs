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

    [Column("is_approved")]
    public bool IsApproved { get; set; } = false;

    [Column("is_disabled")]
    public bool IsDisabled { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [StringLength(45)]
    [Column("last_login_ip")]
    public string? LastLoginIp { get; set; }

    [StringLength(45)]
    [Column("last_failed_login_ip")]
    public string? LastFailedLoginIp { get; set; }

    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; }

    [Column("lockout_end_utc")]
    public DateTime? LockoutEndUtc { get; set; }

    [Column("must_reset_password")]
    public bool MustResetPassword { get; set; }

    [Column("is_suspended")]
    public bool IsSuspended { get; set; }

    [Column("suspended_at_utc")]
    public DateTime? SuspendedAtUtc { get; set; }

    [StringLength(255)]
    [Column("suspension_reason")]
    public string? SuspensionReason { get; set; }
}
