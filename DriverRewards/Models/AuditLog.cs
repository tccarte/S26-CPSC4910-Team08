using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_audit_logs")]
public class AuditLog
{
    [Key]
    [Column("audit_log_id")]
    public long AuditLogId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("entity_type")]
    public string? EntityType { get; set; }

    [StringLength(100)]
    [Column("entity_id")]
    public string? EntityId { get; set; }

    [StringLength(30)]
    [Column("actor_type")]
    public string? ActorType { get; set; }

    [StringLength(100)]
    [Column("actor_id")]
    public string? ActorId { get; set; }

    [StringLength(150)]
    [Column("actor_name")]
    public string? ActorName { get; set; }

    [StringLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("changes_json", TypeName = "longtext")]
    public string? ChangesJson { get; set; }

    [Column("metadata_json", TypeName = "longtext")]
    public string? MetadataJson { get; set; }

    [StringLength(255)]
    [Column("request_path")]
    public string? RequestPath { get; set; }

    [StringLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("occurred_at")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
