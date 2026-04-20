using DriverRewards.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DriverRewards.Pages.Admin;

[Authorize(Roles = "Admin")]
public class MaintenanceModel : PageModel
{
    [BindProperty]
    public DateTime EndLocal { get; set; } = DateTime.Now;

    public bool IsActive { get; set; }
    public string CurrentEndDisplay { get; set; } = "Not scheduled";
    public string? StatusMessage { get; set; }
    public string DefaultEndLocalValue { get; set; } = string.Empty;

    public void OnGet()
    {
        if (EndLocal.Year <= 1)
        {
            EndLocal = DateTime.Now;
        }

        LoadState();
    }

    public IActionResult OnPostStart()
    {
        if (EndLocal.Year <= 1)
        {
            EndLocal = DateTime.Now;
            StatusMessage = "Please choose a valid end time.";
            LoadState();
            return Page();
        }

        if (EndLocal <= DateTime.Now)
        {
            EndLocal = DateTime.Now;
            StatusMessage = "End time must be later than the current time.";
            LoadState();
            return Page();
        }

        var endUtc = DateTime.SpecifyKind(EndLocal, DateTimeKind.Local).ToUniversalTime();
        MaintenanceState.Start(endUtc);

        StatusMessage = "Maintenance mode enabled.";
        LoadState();
        return Page();
    }

    public IActionResult OnPostStop()
    {
        MaintenanceState.Stop();
        EndLocal = DateTime.Now;
        StatusMessage = "Maintenance mode ended.";
        LoadState();
        return Page();
    }

    private void LoadState()
    {
        IsActive = MaintenanceState.IsActive();

        if (MaintenanceState.EndUtc.HasValue)
        {
            CurrentEndDisplay = DateTime
                .SpecifyKind(MaintenanceState.EndUtc.Value, DateTimeKind.Utc)
                .ToLocalTime()
                .ToString("MM/dd/yyyy HH:mm:ss");
        }
        else
        {
            CurrentEndDisplay = "Not scheduled";
        }

        if (EndLocal.Year <= 1)
        {
            EndLocal = DateTime.Now;
        }

        DefaultEndLocalValue = EndLocal.ToString("yyyy-MM-ddTHH:mm");
    }
}