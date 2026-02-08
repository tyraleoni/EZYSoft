using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
 public class LogoutModel : PageModel
 {
 private readonly SignInManager<ApplicationUser> _signInManager;
 private readonly AuthDbContext _db;

 public LogoutModel(SignInManager<ApplicationUser> signInManager, AuthDbContext db)
 {
 _signInManager = signInManager; _db = db;
 }

 public async Task<IActionResult> OnPostAsync()
 {
 // find session key in server session and mark inactive
 var sessionKey = HttpContext.Session.GetString("SessionKey");
 if (!string.IsNullOrEmpty(sessionKey))
 {
 var s = _db.UserSessions.FirstOrDefault(x => x.SessionKey == sessionKey && x.IsActive);
 if (s != null)
 {
 s.IsActive = false;
 _db.SaveChanges();
 }
 HttpContext.Session.Remove("SessionKey");
 }

 await _signInManager.SignOutAsync();
 return RedirectToPage("/Index");
 }
 }
}