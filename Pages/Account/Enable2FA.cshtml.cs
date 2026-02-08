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
 public string? QrCodeImage { get; set; }
 public string? SharedKey { get; set; }
 [BindProperty]
 public string Code { get; set; }

 public Enable2FAModel(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

 public async Task OnGetAsync()
 {
 var user = await _userManager.GetUserAsync(User);
 if (user == null) RedirectToPage("/Account/Login");
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
 }

 public async Task<IActionResult> OnPostAsync()
 {
 var user = await _userManager.GetUserAsync(User);
 if (user == null) return RedirectToPage("/Account/Login");
 var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, Code);
 if (!isValid)
 {
 ModelState.AddModelError(string.Empty, "Invalid verification code.");
 await OnGetAsync();
 return Page();
 }
 await _userManager.SetTwoFactorEnabledAsync(user, true);
 return RedirectToPage("/Index");
 }
 }
}
