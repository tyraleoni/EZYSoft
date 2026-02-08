using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using WebApplication1.Services;
using System.Text.Encodings.Web;

namespace WebApplication1.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly AuthDbContext _db;

        public ApplicationUser CurrentUser { get; set; }
   public bool IsEmailConfirmed { get; set; }
        public string Message { get; set; }

 public ProfileModel(UserManager<ApplicationUser> userManager, IEmailService emailService, AuthDbContext db)
   {
            _userManager = userManager;
        _emailService = emailService;
_db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
   CurrentUser = await _userManager.GetUserAsync(User);
   if (CurrentUser == null)
   {
            return RedirectToPage("/Account/Login");
            }

 IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(CurrentUser);
         return Page();
        }

 public async Task<IActionResult> OnPostResendConfirmationEmailAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
          if (CurrentUser == null)
         {
         return RedirectToPage("/Account/Login");
      }

       // Check if already confirmed
      if (await _userManager.IsEmailConfirmedAsync(CurrentUser))
            {
      Message = "Your email is already confirmed!";
       IsEmailConfirmed = true;
       return Page();
            }

        // Generate new confirmation token
       var token = await _userManager.GenerateEmailConfirmationTokenAsync(CurrentUser);
    var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { userId = CurrentUser.Id, code = token }, protocol: Request.Scheme);
   
            var emailBody = $@"
<html>
  <body>
  <h2>Confirm Your Email - Ace Job Agency</h2>
        <p>Please confirm your email address by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</p>
        <p>Or copy this link: {HtmlEncoder.Default.Encode(callbackUrl)}</p>
        <p>If you did not request this, please ignore this email.</p>
    </body>
</html>";

            try
  {
    await _emailService.SendEmailAsync(CurrentUser.Email, "Confirm your email - Ace Job Agency", emailBody);
      Message = "Confirmation email has been sent to " + CurrentUser.Email;
   _db.AuditLogs.Add(new AuditLog { UserId = CurrentUser.Id, Action = "ResendConfirmationEmail", Timestamp = DateTime.UtcNow });
         await _db.SaveChangesAsync();
            }
catch (Exception ex)
       {
        Message = "Failed to send confirmation email. Please try again later.";
                _db.AuditLogs.Add(new AuditLog { UserId = CurrentUser.Id, Action = "ResendConfirmationEmailFailed", Timestamp = DateTime.UtcNow });
        await _db.SaveChangesAsync();
   }

          IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(CurrentUser);
   return Page();
        }
    }
}
