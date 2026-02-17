using System.ComponentModel.DataAnnotations;
using DriverRewards.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AdminEntity = DriverRewards.Models.Admin;

namespace DriverRewards.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateAdminModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateAdminModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required]
        [StringLength(50)]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? StatusMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var emailExists = await _context.Admins.AsNoTracking()
                .AnyAsync(a => a.Email == Email.Trim());
            if (emailExists)
            {
                ModelState.AddModelError("Email", "An admin with this email already exists.");
                return Page();
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

            var admin = new AdminEntity
            {
                DisplayName = DisplayName.Trim(),
                Email = Email.Trim(),
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            StatusMessage = $"Admin account for '{DisplayName.Trim()}' created successfully.";
            ModelState.Clear();
            DisplayName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            return Page();
        }
    }
}
