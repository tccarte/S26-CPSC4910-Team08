using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriverRewards.Models;

[Table("team08_drivers")]
public class Driver
{
    [Key]
    [Column("driver_id")]
    public int DriverId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("sponsor")]
    public string Sponsor { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [StringLength(50)]
    [Column("fedex_id")]
    public string? FedexId { get; set; }

    [StringLength(100)]
    [Column("shipping_full_name")]
    public string? ShippingFullName { get; set; }

    [StringLength(120)]
    [Column("shipping_address_line1")]
    public string? ShippingAddressLine1 { get; set; }

    [StringLength(120)]
    [Column("shipping_address_line2")]
    public string? ShippingAddressLine2 { get; set; }

    [StringLength(80)]
    [Column("shipping_city")]
    public string? ShippingCity { get; set; }

    [StringLength(80)]
    [Column("shipping_state")]
    public string? ShippingState { get; set; }

    [StringLength(20)]
    [Column("shipping_postal_code")]
    public string? ShippingPostalCode { get; set; }

    [StringLength(80)]
    [Column("shipping_country")]
    public string? ShippingCountry { get; set; }

    [Column("is_approved")]
    public bool IsApproved { get; set; } = true;

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

    [Column("num_points")]
    public int? NumPoints { get; set; }

    [Column("notify_email_points")]
    public bool NotifyEmailPoints { get; set; } = true;

    [Column("notify_sms_points")]
    public bool NotifySmsPoints { get; set; } = false;

    [Column("notify_email_order")]
    public bool NotifyEmailOrder { get; set; } = true;

    [Column("notify_sms_order")]
    public bool NotifySmsOrder { get; set; } = false;
}
