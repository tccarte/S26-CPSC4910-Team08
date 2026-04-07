using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_user_sessions")]
public class UserSession
{
    [Key]
    [Column("user_session_id")]
    public long UserSessionId { get; set; }

    [Required]
    [StringLength(30)]
    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(64)]
    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [StringLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("last_seen_at_utc")]
    public DateTime? LastSeenAtUtc { get; set; }

    [Column("expires_at_utc")]
    public DateTime ExpiresAtUtc { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }

    [Column("revoked_at_utc")]
    public DateTime? RevokedAtUtc { get; set; }

    [StringLength(255)]
    [Column("revoke_reason")]
    public string? RevokeReason { get; set; }
}
