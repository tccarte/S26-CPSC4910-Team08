using Microsoft.EntityFrameworkCore;
using DriverRewards.Models;
using DriverRewards.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DriverRewards.Data;

public class ApplicationDbContext : DbContext
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAuditActorProvider? _auditActorProvider;
    private bool _isWritingAuditLogs;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IAuditActorProvider? auditActorProvider = null)
        : base(options)
    {
        _auditActorProvider = auditActorProvider;
    }

    public DbSet<Admin> Admins { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Behavior> Behaviors { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<SponsorCatalogProduct> SponsorCatalogProducts { get; set; }
    public DbSet<SponsorChangeRequest> SponsorChangeRequests { get; set; }
    public DbSet<DriverNotification> DriverNotifications { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.OccurredAt);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.Category, a.OccurredAt });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.ActorType, a.OccurredAt });

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => s.SessionId)
            .IsUnique();

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => new { s.Role, s.UserId, s.IsRevoked, s.ExpiresAtUtc });

        modelBuilder.Entity<SponsorCatalogProduct>()
            .HasIndex(scp => new { scp.SponsorId, scp.ProductId })
            .IsUnique();

        modelBuilder.Entity<SponsorCatalogProduct>()
            .HasOne(scp => scp.Sponsor)
            .WithMany()
            .HasForeignKey(scp => scp.SponsorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Driver)
            .WithMany()
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DriverNotification>()
            .HasOne(n => n.Driver)
            .WithMany()
            .HasForeignKey(n => n.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (_isWritingAuditLogs)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var pendingAuditEntries = BuildPendingAuditEntries();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (pendingAuditEntries.Count == 0)
        {
            return result;
        }

        try
        {
            _isWritingAuditLogs = true;
            foreach (var pending in pendingAuditEntries)
            {
                foreach (var property in pending.TemporaryProperties)
                {
                    if (property.Metadata.IsPrimaryKey())
                    {
                        pending.EntityId = property.CurrentValue?.ToString();
                    }
                }

                AuditLogs.Add(new AuditLog
                {
                    Category = "DataChange",
                    Action = pending.Action,
                    EntityType = pending.EntityType,
                    EntityId = pending.EntityId,
                    ActorType = pending.Actor.ActorType,
                    ActorId = pending.Actor.ActorId,
                    ActorName = pending.Actor.ActorName,
                    Description = pending.Description,
                    ChangesJson = pending.Changes.Count == 0 ? null : JsonSerializer.Serialize(pending.Changes, AuditJsonOptions),
                    MetadataJson = pending.Metadata.Count == 0 ? null : JsonSerializer.Serialize(pending.Metadata, AuditJsonOptions),
                    RequestPath = pending.Actor.RequestPath,
                    IpAddress = pending.Actor.IpAddress,
                    OccurredAt = DateTime.UtcNow
                });
            }

            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        finally
        {
            _isWritingAuditLogs = false;
        }

        return result;
    }

    private List<PendingAuditEntry> BuildPendingAuditEntries()
    {
        ChangeTracker.DetectChanges();

        var actor = _auditActorProvider?.GetCurrentActor() ?? new AuditActorInfo();
        var pendingEntries = new List<PendingAuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var pending = new PendingAuditEntry(entry, actor)
            {
                EntityType = entry.Metadata.ClrType.Name,
                Action = entry.State.ToString(),
                Description = BuildDescription(entry)
            };

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsShadowProperty())
                {
                    continue;
                }

                var propertyName = property.Metadata.Name;

                if (property.Metadata.IsPrimaryKey())
                {
                    if (property.IsTemporary)
                    {
                        pending.TemporaryProperties.Add(property);
                    }
                    else
                    {
                        pending.EntityId = property.CurrentValue?.ToString();
                    }

                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        pending.Changes[propertyName] = new { New = ToAuditValue(propertyName, property.CurrentValue) };
                        break;
                    case EntityState.Deleted:
                        pending.Changes[propertyName] = new { Old = ToAuditValue(propertyName, property.OriginalValue) };
                        break;
                    case EntityState.Modified:
                        if (!property.IsModified)
                        {
                            break;
                        }

                        var originalValue = ToAuditValue(propertyName, property.OriginalValue);
                        var currentValue = ToAuditValue(propertyName, property.CurrentValue);
                        if (Equals(originalValue, currentValue))
                        {
                            break;
                        }

                        pending.Changes[propertyName] = new { Old = originalValue, New = currentValue };
                        break;
                }
            }

            if (pending.Changes.Count == 0 && entry.State == EntityState.Modified)
            {
                continue;
            }

            pending.Metadata["State"] = entry.State.ToString();
            pendingEntries.Add(pending);
        }

        return pendingEntries;
    }

    private static object? ToAuditValue(string propertyName, object? value)
    {
        if (IsSensitiveProperty(propertyName))
        {
            return "[REDACTED]";
        }

        return value switch
        {
            null => null,
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            decimal dec => dec,
            _ => value
        };
    }

    private static bool IsSensitiveProperty(string propertyName)
    {
        return propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDescription(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => $"{entry.Metadata.ClrType.Name} created.",
            EntityState.Modified => $"{entry.Metadata.ClrType.Name} updated.",
            EntityState.Deleted => $"{entry.Metadata.ClrType.Name} deleted.",
            _ => $"{entry.Metadata.ClrType.Name} changed."
        };
    }

    private sealed class PendingAuditEntry
    {
        public PendingAuditEntry(EntityEntry entry, AuditActorInfo actor)
        {
            Entry = entry;
            Actor = actor;
        }

        public EntityEntry Entry { get; }
        public AuditActorInfo Actor { get; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object> Changes { get; } = new();
        public Dictionary<string, object> Metadata { get; } = new();
        public List<PropertyEntry> TemporaryProperties { get; } = new();
    }
}
