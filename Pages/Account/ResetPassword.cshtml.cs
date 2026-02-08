using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
 public class ResetPasswordModel : PageModel
 {
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly AuthDbContext _db;
 public ResetPasswordModel(UserManager<ApplicationUser> userManager, AuthDbContext db) { _userManager = userManager; _db = db; }

 [BindProperty]
 public string UserId { get; set; }
 [BindProperty]
 public string Token { get; set; }
 [BindProperty]
 public string NewPassword { get; set; }
 [BindProperty]
 public string ConfirmPassword { get; set; }
 public string Message { get; set; }

 public void OnGet(string userId, string token)
 {
 UserId = userId; Token = token;
 }

 public async Task<IActionResult> OnPostAsync()
 {
 if (NewPassword != ConfirmPassword) { ModelState.AddModelError(string.Empty, "Passwords do not match"); return Page(); }
 var user = await _userManager.FindByIdAsync(UserId);
 if (user == null) { ModelState.AddModelError(string.Empty, "Invalid token or user"); return Page(); }
 var res = await _userManager.ResetPasswordAsync(user, Token, NewPassword);
 if (!res.Succeeded) { foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description); return Page(); }

 // re-fetch user to get the updated password hash stored by Identity
 var updated = await _userManager.FindByIdAsync(UserId);
 var newHash = updated?.PasswordHash ?? user.PasswordHash;

 // add to history using the actual stored hash
 _db.PasswordHistories.Add(new PasswordHistory { UserId = user.Id, PasswordHash = newHash, ChangedAt = DateTime.UtcNow });
 _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "PasswordReset", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 Message = "Password reset successfully.";
 return Page();
 }
 }
}