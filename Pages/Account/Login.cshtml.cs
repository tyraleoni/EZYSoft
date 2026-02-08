using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
 public class LoginModel : PageModel
 {
 private readonly SignInManager<ApplicationUser> _signInManager;
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly AuthDbContext _db;

 public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthDbContext db)
 {
 _signInManager = signInManager;
 _userManager = userManager;
 _db = db;
 }

 [BindProperty]
 public string Email { get; set; }

 [BindProperty]
 public string Password { get; set; }

 public string ErrorMessage { get; set; }

 [TempData]
 public string? TwoFaUserId { get; set; }

 public void OnGet()
 {
 }

 public async Task<IActionResult> OnPostAsync()
 {
 if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
 {
 ModelState.AddModelError(string.Empty, "Email and password are required.");
 return Page();
 }

 var user = await _userManager.FindByEmailAsync(Email);
 if (user == null)
 {
 // audit log: failed login (unknown user)
 _db.AuditLogs.Add(new AuditLog { UserId = "(unknown)", Action = "LoginFailure", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 ModelState.AddModelError(string.Empty, "Invalid login attempt.");
 return Page();
 }

 var result = await _signInManager.CheckPasswordSignInAsync(user, Password, lockoutOnFailure: true);

 if (result.Succeeded)
 {
 if (await _userManager.GetTwoFactorEnabledAsync(user))
 {
 // require2FA: store temp user id and redirect
 TwoFaUserId = user.Id;
 await _db.SaveChangesAsync();
 return RedirectToPage("/Account/Verify2FA");
 }

 // create session record similar to registration
 var sessionKey = Guid.NewGuid().ToString();
 var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
 var ua = Request.Headers["User-Agent"].ToString();
 var sess = new UserSession { UserId = user.Id, SessionKey = sessionKey, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(30), IpAddress = ip, UserAgent = ua, IsActive = true };
 _db.UserSessions.Add(sess);
 _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LoginSuccess", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 HttpContext.Session.SetString("SessionKey", sessionKey);

 await _signInManager.SignInAsync(user, isPersistent: false);

 return RedirectToPage("/Index");
 }

 if (result.IsLockedOut)
 {
 _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "Lockout", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();
 ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Try again later.");
 return Page();
 }

 // not succeeded
 _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LoginFailure", Timestamp = DateTime.UtcNow });
 await _db.SaveChangesAsync();

 ModelState.AddModelError(string.Empty, "Invalid login attempt.");
 return Page();
 }
 }
}
