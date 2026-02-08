using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
 public class Verify2FAModel : PageModel
 {
 private readonly SignInManager<ApplicationUser> _signInManager;
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly AuthDbContext _db;

 public Verify2FAModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthDbContext db)
 {
 _signInManager = signInManager; _userManager = userManager; _db = db;
 }

 [BindProperty]
 public string Code { get; set; }

 // The temporary user id could be stored in TempData during initial sign in
 [TempData]
 public string? TwoFaUserId { get; set; }

 public void OnGet() { }

 public async Task<IActionResult> OnPostAsync()
 {
 if (string.IsNullOrEmpty(TwoFaUserId)) return RedirectToPage("/Account/Login");
 var user = await _userManager.FindByIdAsync(TwoFaUserId);
 if (user == null) return RedirectToPage("/Account/Login");

 var valid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, Code);
 if (!valid)
 {
 ModelState.AddModelError(string.Empty, "Invalid code.");
 return Page();
 }

 // sign in
 await _signInManager.SignInAsync(user, isPersistent: false);
 _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "Login2FASuccess", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 return RedirectToPage("/Account/Profile");
 }
 }
}
