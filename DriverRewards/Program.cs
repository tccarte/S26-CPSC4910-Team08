using DriverRewards.Data;
using DriverRewards.Models;
using DriverRewards.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ProductCatalogService>();
builder.Services.AddHttpClient<ShippingTrackingService>();
builder.Services.AddScoped<IAuditActorProvider, AuditActorProvider>();
builder.Services.AddScoped<AuditService>();
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

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
