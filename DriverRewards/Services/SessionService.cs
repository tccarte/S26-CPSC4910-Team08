using System.Security.Claims;
using DriverRewards.Data;
using DriverRewards.Models;
using Microsoft.EntityFrameworkCore;

namespace DriverRewards.Services;

public class SessionService
{
    private readonly ApplicationDbContext _context;

    public SessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateSessionAsync(
        string role,
        int userId,
        string sessionId,
        string? ipAddress,
        string? userAgent,
        DateTime expiresAtUtc)
    {
        var session = new UserSession
        {
            Role = role,
            UserId = userId,
            SessionId = sessionId,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeSessionAsync(string sessionId, string? reason = null)
    {
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session == null || session.IsRevoked)
        {
            return;
        }

        session.IsRevoked = true;
        session.RevokedAtUtc = DateTime.UtcNow;
        session.RevokeReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _context.SaveChangesAsync();
    }

    public async Task<int> RevokeAllSessionsAsync(string role, int userId, string? reason = null)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.Role == role && s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAtUtc = now;
            session.RevokeReason = string.IsNullOrWhiteSpace(reason) ? "Revoked by admin." : reason.Trim();
        }

        await _context.SaveChangesAsync();
        return sessions.Count;
    }

    public static string? GetSessionId(ClaimsPrincipal user)
    {
        return user.FindFirstValue("sid");
    }
}
