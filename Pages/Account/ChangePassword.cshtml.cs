using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
 public class ChangePasswordModel : PageModel
 {
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly AuthDbContext _db;
 private readonly IConfiguration _config;
 private readonly IPasswordHasher<ApplicationUser> _hasher;

 public ChangePasswordModel(UserManager<ApplicationUser> userManager, AuthDbContext db, IConfiguration config, IPasswordHasher<ApplicationUser> hasher)
 {
 _userManager = userManager; _db = db; _config = config; _hasher = hasher;
 }

 [BindProperty]
 public string CurrentPassword { get; set; }
 [BindProperty]
 public string NewPassword { get; set; }
 [BindProperty]
 public string ConfirmPassword { get; set; }

 public string Message { get; set; }

 public void OnGet()
 {
 }

 public async Task<IActionResult> OnPostAsync()
 {
 if (NewPassword != ConfirmPassword)
 {
 ModelState.AddModelError(string.Empty, "New password and confirmation do not match.");
 return Page();
 }

 var user = await _userManager.GetUserAsync(User);
 if (user == null) return RedirectToPage("/Account/Login");

 // Enforce minimum minutes between changes
 var minMins = _config.GetValue<int>("PasswordPolicy:MinChangeMinutes");
 var lastChange = _db.PasswordHistories.Where(p => p.UserId == user.Id).OrderByDescending(p => p.ChangedAt).FirstOrDefault();
 if (lastChange != null && (DateTime.UtcNow - lastChange.ChangedAt).TotalMinutes < minMins)
 {
 ModelState.AddModelError(string.Empty, $"You can only change password once every {minMins} minutes.");
 return Page();
 }

 // Prevent reuse against last2
 var recent = _db.PasswordHistories.Where(p => p.UserId == user.Id).OrderByDescending(p => p.ChangedAt).Take(2).ToList();
 foreach (var h in recent)
 {
 var verify = _hasher.VerifyHashedPassword(user, h.PasswordHash, NewPassword);
 if (verify == PasswordVerificationResult.Success)
 {
 ModelState.AddModelError(string.Empty, "You cannot reuse a recent password.");
 return Page();
 }
 }

 var res = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
 if (!res.Succeeded)
 {
 foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
 return Page();
 }

 // add to history
 _db.PasswordHistories.Add(new PasswordHistory { UserId = user.Id, PasswordHash = user.PasswordHash, ChangedAt = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 Message = "Password changed successfully.";
 return Page();
 }
 }
}