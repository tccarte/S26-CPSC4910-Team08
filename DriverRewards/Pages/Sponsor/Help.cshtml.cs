using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DriverRewards.Pages.Sponsor;

[Authorize(Roles = "Sponsor")]
public class HelpModel : PageModel
{
    public void OnGet() { }
}
