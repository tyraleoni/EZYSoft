using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Pages.Account
{
 public class ForgotPasswordModel : PageModel
 {
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly IConfiguration _config;
 private readonly AuthDbContext _db;
 private readonly IEmailService _email;

 public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IConfiguration config, AuthDbContext db, IEmailService email)
 {
 _userManager = userManager; _config = config; _db = db; _email = email;
 }

 [BindProperty]
 public string Email { get; set; }
 public string Message { get; set; }

 public void OnGet() { }

 public async Task<IActionResult> OnPostAsync()
 {
 if (string.IsNullOrEmpty(Email)) { ModelState.AddModelError(string.Empty, "Email required"); return Page(); }
 var user = await _userManager.FindByEmailAsync(Email);
 if (user == null)
 {
 // Don't reveal whether email exists - show generic message
 Message = "If an account with that email exists, a reset link has been sent.";
 return Page();
 }

 var token = await _userManager.GeneratePasswordResetTokenAsync(user);
 var resetUrl = Url.Page("/Account/ResetPassword", null, new { userId = user.Id, token = token }, Request.Scheme);

 // send email
 var body = $"<p>Please reset your password by <a href=\"{resetUrl}\">clicking here</a>.</p>";
 await _email.SendEmailAsync(Email, "Password reset", body);

 _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "PasswordResetRequested", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 // Show same message regardless of whether user exists (prevents account enumeration)
 Message = "If an account with that email exists, a reset link has been sent.";
 return Page();
 }
 }
}