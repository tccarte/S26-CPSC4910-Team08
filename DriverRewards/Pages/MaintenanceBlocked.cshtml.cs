using DriverRewards.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DriverRewards.Pages;

public class MaintenanceBlockedModel : PageModel
{
    public string EndDisplay { get; set; } = "Unknown";

    public void OnGet()
    {
        if (MaintenanceState.EndUtc.HasValue)
        {
            EndDisplay = DateTime
                .SpecifyKind(MaintenanceState.EndUtc.Value, DateTimeKind.Utc)
                .ToLocalTime()
                .ToString("MM/dd/yyyy HH:mm:ss");
        }
    }
}