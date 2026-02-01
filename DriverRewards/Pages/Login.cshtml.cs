using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DriverRewards.Pages;

public class LoginModel : PageModel
{
    [BindProperty]
    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [Display(Name = "Account type")]
    public string Role { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Please fix the errors and try again.";
            return;
        }

        // Placeholder for real authentication logic.
        StatusMessage = "Login submitted. Wire this up to your auth system.";
    }
}
