using System.Security.Claims;

namespace DriverRewards.Services;

public interface IAuditActorProvider
{
    AuditActorInfo GetCurrentActor();
}

public sealed class AuditActorInfo
{
    public string? ActorType { get; init; }
    public string? ActorId { get; init; }
    public string? ActorName { get; init; }
    public string? RequestPath { get; init; }
    public string? IpAddress { get; init; }
}

public class AuditActorProvider : IAuditActorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditActorProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditActorInfo GetCurrentActor()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        var actorType = user?.FindFirstValue(ClaimTypes.Role);
        var actorName = user?.Identity?.Name ?? user?.FindFirstValue(ClaimTypes.Name);
        var actorId = GetActorId(user?.FindFirstValue(ClaimTypes.NameIdentifier));

        return new AuditActorInfo
        {
            ActorType = actorType,
            ActorId = actorId,
            ActorName = actorName,
            RequestPath = httpContext?.Request.Path.Value,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString()
        };
    }

    private static string? GetActorId(string? rawIdentifier)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier))
        {
            return null;
        }

        var parts = rawIdentifier.Split(':', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : rawIdentifier;
    }
}
