using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using System.Text; 
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
 public class Enable2FAModel : PageModel
 {
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly SignInManager<ApplicationUser> _signInManager;
  private readonly AuthDbContext _db;

 public string? QrCodeImage { get; set; }
 public string? SharedKey { get; set; }
 public bool IsRequiredSetup { get; set; }

 [BindProperty]
 public string Code { get; set; }

 public Enable2FAModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AuthDbContext db) 
 { 
  _userManager = userManager;
   _signInManager = signInManager;
   _db = db;
 }

 public async Task<IActionResult> OnGetAsync(bool requiredSetup = false)
 {
  var user = await _userManager.GetUserAsync(User);
if (user == null)
 {
   // If no authenticated user and requiredSetup is true, we need a temp user
if (!requiredSetup)
      {
    return RedirectToPage("/Account/Login");
          }
      // For required setup after login attempt, user should be authenticated
    return RedirectToPage("/Account/Login");
}

 IsRequiredSetup = requiredSetup;

 var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
         if (string.IsNullOrEmpty(unformattedKey))
       {
   await _userManager.ResetAuthenticatorKeyAsync(user);
       unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
      }
 SharedKey = unformattedKey;
    var email = user.Email;
      var issuer = System.Uri.EscapeDataString("EZYSoft");
  var userPart = System.Uri.EscapeDataString(email ?? user.UserName ?? "user");
      var uri = $"otpauth://totp/{issuer}:{userPart}?secret={unformattedKey}&issuer={issuer}&digits=6";
    using var qr = new QRCodeGenerator();
     var qrData = qr.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
       var qrCode = new PngByteQRCode(qrData);
   var qrBytes = qrCode.GetGraphic(20);
      QrCodeImage = "data:image/png;base64," + Convert.ToBase64String(qrBytes);

   return Page();
 }

 public async Task<IActionResult> OnPostAsync(bool requiredSetup = false)
 {
  var user = await _userManager.GetUserAsync(User);
         if (user == null) return RedirectToPage("/Account/Login");

  IsRequiredSetup = requiredSetup;

    var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, Code);
      if (!isValid)
 {
      ModelState.AddModelError(string.Empty, "Invalid verification code.");
    await OnGetAsync(requiredSetup);
   return Page();
      }

    await _userManager.SetTwoFactorEnabledAsync(user, true);
         _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "2FAEnabled", Timestamp = DateTime.UtcNow });
          await _db.SaveChangesAsync();

       if (requiredSetup)
  {
    // Sign in the user now that 2FA is enabled
   await _signInManager.SignInAsync(user, isPersistent: false);
     // Create session record
  var sessionKey = Guid.NewGuid().ToString();
          var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
       var ua = HttpContext.Request.Headers["User-Agent"].ToString();
 var sess = new UserSession { UserId = user.Id, SessionKey = sessionKey, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(30), IpAddress = ip, UserAgent = ua, IsActive = true };
   _db.UserSessions.Add(sess);
   _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LoginSuccess", Timestamp = DateTime.UtcNow });
        await _db.SaveChangesAsync();
  HttpContext.Session.SetString("SessionKey", sessionKey);
     }

   return RedirectToPage("/Account/Profile");
 }
 }
}
