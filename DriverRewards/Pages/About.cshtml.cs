using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DriverRewards.Data;

namespace DriverRewards.Pages;

public class AboutModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public bool DatabaseConnected { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int DriverCount { get; set; }
    public int SponsorCount { get; set; }
    public string ConnectionError { get; set; } = string.Empty;

    public AboutModel(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                    DatabaseServer = trimmed.Substring("Server=".Length);
                else if (trimmed.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    DatabaseName = trimmed.Substring("Database=".Length);
            }

            DriverCount = await _context.Drivers.CountAsync();
            SponsorCount = await _context.Sponsors.CountAsync();
            DatabaseConnected = true;
        }
        catch (Exception ex)
        {
            DatabaseConnected = false;
            ConnectionError = ex.Message;
        }
    }
}
