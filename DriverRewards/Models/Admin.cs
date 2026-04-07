using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_admins")]
public class Admin
{
    [Key]
    [Column("admin_id")]
    public int AdminId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

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
