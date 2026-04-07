using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Configure MySQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.SlidingExpiration = false;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                if (context.Principal?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                var sessionId = context.Principal.FindFirstValue("sid");
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                if (!TryParseRoleAndUserId(context.Principal, out var role, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var session = await db.UserSessions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null
                    || session.IsRevoked
                    || session.ExpiresAtUtc <= DateTime.UtcNow
                    || !string.Equals(session.Role, role, StringComparison.OrdinalIgnoreCase)
                    || session.UserId != userId)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var isSuspended = role switch
                {
                    "Driver" => await db.Drivers.AsNoTracking()
                        .Where(d => d.DriverId == userId)
                        .Select(d => d.IsSuspended)
                        .FirstOrDefaultAsync(),
                    "Sponsor" => await db.Sponsors.AsNoTracking()
                        .Where(s => s.SponsorId == userId)
                        .Select(s => s.IsSuspended)
                        .FirstOrDefaultAsync(),
                    "Admin" => await db.Admins.AsNoTracking()
                        .Where(a => a.AdminId == userId)
                        .Select(a => a.IsSuspended)
                        .FirstOrDefaultAsync(),
                    _ => true
                };

                if (isSuspended)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ProductCatalogService>();
builder.Services.AddHttpClient<ShippingTrackingService>();
builder.Services.AddScoped<IAuditActorProvider, AuditActorProvider>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<DriverRewards.Services.NotificationService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS team08_audit_logs (
            audit_log_id bigint NOT NULL AUTO_INCREMENT,
            category varchar(50) NOT NULL,
            action varchar(50) NOT NULL,
            entity_type varchar(100) NULL,
            entity_id varchar(100) NULL,
            actor_type varchar(30) NULL,
            actor_id varchar(100) NULL,
            actor_name varchar(150) NULL,
            description varchar(500) NULL,
            changes_json longtext NULL,
            metadata_json longtext NULL,
            request_path varchar(255) NULL,
            ip_address varchar(45) NULL,
            occurred_at datetime(6) NOT NULL,
            PRIMARY KEY (audit_log_id),
            INDEX IX_team08_audit_logs_occurred_at (occurred_at),
            INDEX IX_team08_audit_logs_category_occurred_at (category, occurred_at),
            INDEX IX_team08_audit_logs_actor_type_occurred_at (actor_type, occurred_at)
        ) CHARACTER SET utf8mb4;
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS team08_user_sessions (
            user_session_id bigint NOT NULL AUTO_INCREMENT,
            role varchar(30) NOT NULL,
            user_id int NOT NULL,
            session_id varchar(64) NOT NULL,
            ip_address varchar(45) NULL,
            user_agent varchar(500) NULL,
            created_at_utc datetime(6) NOT NULL,
            last_seen_at_utc datetime(6) NULL,
            expires_at_utc datetime(6) NOT NULL,
            is_revoked tinyint(1) NOT NULL DEFAULT 0,
            revoked_at_utc datetime(6) NULL,
            revoke_reason varchar(255) NULL,
            PRIMARY KEY (user_session_id),
            UNIQUE KEY UX_team08_user_sessions_session_id (session_id),
            INDEX IX_team08_user_sessions_role_user_active (role, user_id, is_revoked, expires_at_utc)
        ) CHARACTER SET utf8mb4;
        """);
    var ensureColumnStatements = new[]
    {
        "ALTER TABLE team08_admins ADD COLUMN last_login_ip varchar(45) NULL;",
        "ALTER TABLE team08_admins ADD COLUMN last_failed_login_ip varchar(45) NULL;",
        "ALTER TABLE team08_admins ADD COLUMN failed_login_attempts int NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_admins ADD COLUMN lockout_end_utc datetime(6) NULL;",
        "ALTER TABLE team08_admins ADD COLUMN must_reset_password tinyint(1) NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_admins ADD COLUMN is_suspended tinyint(1) NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_admins ADD COLUMN suspended_at_utc datetime(6) NULL;",
        "ALTER TABLE team08_admins ADD COLUMN suspension_reason varchar(255) NULL;",

        "ALTER TABLE team08_drivers ADD COLUMN last_login_ip varchar(45) NULL;",
        "ALTER TABLE team08_drivers ADD COLUMN last_failed_login_ip varchar(45) NULL;",
        "ALTER TABLE team08_drivers ADD COLUMN failed_login_attempts int NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_drivers ADD COLUMN lockout_end_utc datetime(6) NULL;",
        "ALTER TABLE team08_drivers ADD COLUMN must_reset_password tinyint(1) NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_drivers ADD COLUMN is_suspended tinyint(1) NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_drivers ADD COLUMN suspended_at_utc datetime(6) NULL;",
        "ALTER TABLE team08_drivers ADD COLUMN suspension_reason varchar(255) NULL;",

        "ALTER TABLE team08_sponsors ADD COLUMN last_login_ip varchar(45) NULL;",
        "ALTER TABLE team08_sponsors ADD COLUMN last_failed_login_ip varchar(45) NULL;",
        "ALTER TABLE team08_sponsors ADD COLUMN failed_login_attempts int NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_sponsors ADD COLUMN lockout_end_utc datetime(6) NULL;",
        "ALTER TABLE team08_sponsors ADD COLUMN must_reset_password tinyint(1) NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_sponsors ADD COLUMN is_suspended tinyint(1) NOT NULL DEFAULT 0;",
        "ALTER TABLE team08_sponsors ADD COLUMN suspended_at_utc datetime(6) NULL;",
        "ALTER TABLE team08_sponsors ADD COLUMN suspension_reason varchar(255) NULL;"
    };

    foreach (var statement in ensureColumnStatements)
    {
        await EnsureColumnAsync(dbContext, statement);
    }
    var hasAnyAdmin = await dbContext.Admins.AnyAsync();

    if (!hasAnyAdmin)
    {
        dbContext.Admins.Add(new Admin
        {
            DisplayName = "Default Admin",
            Email = "admin@driverrewards.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true
        && !Path.HasExtension(context.Request.Path.Value))
    {
        var path = context.Request.Path;
        var bypass = path.StartsWithSegments("/ChangePassword")
            || path.StartsWithSegments("/Logout")
            || path.StartsWithSegments("/Login");

        if (!bypass && TryParseRoleAndUserId(context.User, out var role, out var userId))
        {
            var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var mustResetPassword = role switch
            {
                "Driver" => await db.Drivers.AsNoTracking()
                    .Where(d => d.DriverId == userId)
                    .Select(d => d.MustResetPassword)
                    .FirstOrDefaultAsync(),
                "Sponsor" => await db.Sponsors.AsNoTracking()
                    .Where(s => s.SponsorId == userId)
                    .Select(s => s.MustResetPassword)
                    .FirstOrDefaultAsync(),
                "Admin" => await db.Admins.AsNoTracking()
                    .Where(a => a.AdminId == userId)
                    .Select(a => a.MustResetPassword)
                    .FirstOrDefaultAsync(),
                _ => false
            };

            if (mustResetPassword)
            {
                context.Response.Redirect("/ChangePassword?required=1");
                return;
            }
        }
    }

    await next();
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static bool TryParseRoleAndUserId(ClaimsPrincipal user, out string role, out int userId)
{
    role = string.Empty;
    userId = 0;

    var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(idClaim))
    {
        return false;
    }

    var parts = idClaim.Split(':', 2, StringSplitOptions.TrimEntries);
    if (parts.Length != 2 || !int.TryParse(parts[1], out userId))
    {
        return false;
    }

    role = parts[0];
    return !string.IsNullOrWhiteSpace(role);
}

static async Task EnsureColumnAsync(ApplicationDbContext dbContext, string alterStatement)
{
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(alterStatement);
    }
    catch (MySqlException ex) when (ex.Number == 1060)
    {
        // Duplicate column name; safe to ignore for idempotent startup schema patching.
    }
}
