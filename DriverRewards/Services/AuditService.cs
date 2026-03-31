using System.Text.Json;
using DriverRewards.Data;
using DriverRewards.Models;

namespace DriverRewards.Services;

public class AuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _context;
    private readonly IAuditActorProvider _actorProvider;

    public AuditService(ApplicationDbContext context, IAuditActorProvider actorProvider)
    {
        _context = context;
        _actorProvider = actorProvider;
    }

    public async Task LogEventAsync(
        string category,
        string action,
        string? description = null,
        string? entityType = null,
        string? entityId = null,
        object? changes = null,
        object? metadata = null)
    {
        var actor = _actorProvider.GetCurrentActor();
        var auditLog = new AuditLog
        {
            Category = category,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorType = actor.ActorType,
            ActorId = actor.ActorId,
            ActorName = actor.ActorName,
            Description = description,
            ChangesJson = changes is null ? null : JsonSerializer.Serialize(changes, JsonOptions),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions),
            RequestPath = actor.RequestPath,
            IpAddress = actor.IpAddress,
            OccurredAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
