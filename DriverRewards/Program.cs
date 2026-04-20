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
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS team08_driver_sponsors (
            id int NOT NULL AUTO_INCREMENT,
            driver_id int NOT NULL,
            sponsor_name varchar(50) NOT NULL,
            is_approved tinyint(1) NOT NULL DEFAULT 0,
            joined_at datetime(6) NOT NULL,
            PRIMARY KEY (id),
            UNIQUE KEY UX_team08_driver_sponsors_driver_sponsor (driver_id, sponsor_name),
            CONSTRAINT FK_team08_driver_sponsors_driver_id
                FOREIGN KEY (driver_id) REFERENCES team08_drivers (driver_id)
                ON DELETE CASCADE
        ) CHARACTER SET utf8mb4;
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        INSERT IGNORE INTO team08_driver_sponsors (driver_id, sponsor_name, is_approved, joined_at)
        SELECT driver_id, sponsor, is_approved, created_at
        FROM team08_drivers
        WHERE sponsor IS NOT NULL AND sponsor != ''
          AND NOT EXISTS (
              SELECT 1 FROM team08_driver_sponsors ds
              WHERE ds.driver_id = team08_drivers.driver_id
          );
        """);

    var ensureColumns = new (string TableName, string ColumnName, string ColumnDefinition)[]
    {
        ("team08_admins", "last_login_ip", "last_login_ip varchar(45) NULL"),
        ("team08_admins", "last_failed_login_ip", "last_failed_login_ip varchar(45) NULL"),
        ("team08_admins", "failed_login_attempts", "failed_login_attempts int NOT NULL DEFAULT 0"),
        ("team08_admins", "lockout_end_utc", "lockout_end_utc datetime(6) NULL"),
        ("team08_admins", "must_reset_password", "must_reset_password tinyint(1) NOT NULL DEFAULT 0"),
        ("team08_admins", "is_suspended", "is_suspended tinyint(1) NOT NULL DEFAULT 0"),
        ("team08_admins", "suspended_at_utc", "suspended_at_utc datetime(6) NULL"),
        ("team08_admins", "suspension_reason", "suspension_reason varchar(255) NULL"),

        ("team08_drivers", "last_login_ip", "last_login_ip varchar(45) NULL"),
        ("team08_drivers", "last_failed_login_ip", "last_failed_login_ip varchar(45) NULL"),
        ("team08_drivers", "failed_login_attempts", "failed_login_attempts int NOT NULL DEFAULT 0"),
        ("team08_drivers", "lockout_end_utc", "lockout_end_utc datetime(6) NULL"),
        ("team08_drivers", "must_reset_password", "must_reset_password tinyint(1) NOT NULL DEFAULT 0"),
        ("team08_drivers", "is_suspended", "is_suspended tinyint(1) NOT NULL DEFAULT 0"),
        ("team08_drivers", "suspended_at_utc", "suspended_at_utc datetime(6) NULL"),
        ("team08_drivers", "suspension_reason", "suspension_reason varchar(255) NULL"),

        ("team08_sponsors", "last_login_ip", "last_login_ip varchar(45) NULL"),
        ("team08_sponsors", "last_failed_login_ip", "last_failed_login_ip varchar(45) NULL"),
        ("team08_sponsors", "failed_login_attempts", "failed_login_attempts int NOT NULL DEFAULT 0"),
        ("team08_sponsors", "lockout_end_utc", "lockout_end_utc datetime(6) NULL"),
        ("team08_sponsors", "must_reset_password", "must_reset_password tinyint(1) NOT NULL DEFAULT 0"),
        ("team08_sponsors", "is_suspended", "is_suspended tinyint(1) NOT NULL DEFAULT 0"),
        ("team08_sponsors", "suspended_at_utc", "suspended_at_utc datetime(6) NULL"),
        ("team08_sponsors", "suspension_reason", "suspension_reason varchar(255) NULL")
    };

    foreach (var column in ensureColumns)
    {
        await EnsureColumnAsync(dbContext, column.TableName, column.ColumnName, column.ColumnDefinition);
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

app.Use(async (context, next) =>
{
    if (!MaintenanceState.IsActive())
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value ?? "";

    if (path.StartsWith("/Admin/Maintenance", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/MaintenanceBlocked", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Logout", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
    var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

    if (isAdmin)
    {
        await next();
        return;
    }

    context.Response.Redirect("/MaintenanceBlocked");
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

static async Task EnsureColumnAsync(
    ApplicationDbContext dbContext,
    string tableName,
    string columnName,
    string columnDefinition)
{
    var connection = (MySqlConnection)dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var existsCommand = connection.CreateCommand();
    existsCommand.CommandText = """
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = @tableName
          AND COLUMN_NAME = @columnName;
        """;
    existsCommand.Parameters.AddWithValue("@tableName", tableName);
    existsCommand.Parameters.AddWithValue("@columnName", columnName);
    var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync()) > 0;
    if (exists)
    {
        return;
    }

    await using var alterCommand = connection.CreateCommand();
    alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnDefinition};";
    await alterCommand.ExecuteNonQueryAsync();
}
