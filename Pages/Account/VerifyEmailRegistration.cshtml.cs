using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
    public class VerifyEmailRegistrationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthDbContext _db;

    [FromQuery]
 public string Email { get; set; }

        [FromQuery]
        public string UserId { get; set; }

        [BindProperty]
        public string VerificationCode { get; set; }

        public string Message { get; set; }
    public string MessageType { get; set; } // "success" or "error"

        public VerifyEmailRegistrationModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AuthDbContext db)
        {
         _userManager = userManager;
            _signInManager = signInManager;
         _db = db;
        }

        public void OnGet()
        {
   }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(VerificationCode))
      {
    Message = "Invalid verification request.";
                MessageType = "error";
         return Page();
            }

          var user = await _userManager.FindByIdAsync(UserId);
    if (user == null)
        {
                Message = "User not found.";
       MessageType = "error";
     return Page();
        }

      // Attempt to confirm email with the provided code
    var result = await _userManager.ConfirmEmailAsync(user, VerificationCode);
        
      if (result.Succeeded)
   {
    // Email confirmed successfully
                _db.AuditLogs.Add(new AuditLog 
              { 
           UserId = user.Id, 
      Action = "EmailConfirmedViaRegistration", 
   Timestamp = DateTime.UtcNow 
});
                await _db.SaveChangesAsync();

    Message = "Email verified successfully! You can now login.";
      MessageType = "success";

                // Auto sign-in the user (optional - remove this line if you want them to login manually)
        await _signInManager.SignInAsync(user, isPersistent: false);

                // Redirect to home page or dashboard
       return RedirectToPage("/Index");
            }
          else
            {
            Message = "Invalid or expired verification code. Please try again.";
  MessageType = "error";
       _db.AuditLogs.Add(new AuditLog 
  { 
          UserId = user.Id, 
         Action = "EmailVerificationFailed", 
          Timestamp = DateTime.UtcNow 
      });
   await _db.SaveChangesAsync();
                return Page();
            }
}
    }
}
