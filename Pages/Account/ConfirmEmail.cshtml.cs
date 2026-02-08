using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;

namespace WebApplication1.Pages.Account
{
    public class ConfirmEmailModel : PageModel
  {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _db;

        public string Message { get; set; }
        public bool Success { get; set; }

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, AuthDbContext db)
        {
            _userManager = userManager;
_db = db;
        }

public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                Message = "Invalid email confirmation link.";
       Success = false;
    return Page();
       }

       var user = await _userManager.FindByIdAsync(userId);
      if (user == null)
    {
           Message = "User not found.";
         Success = false;
       return Page();
 }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
      {
                Message = "Thank you for confirming your email! You can now login.";
     Success = true;
    _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "EmailConfirmed", Timestamp = DateTime.UtcNow });
  await _db.SaveChangesAsync();
        }
       else
            {
    Message = "Email confirmation failed. The link may have expired.";
  Success = false;
          _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "EmailConfirmationFailed", Timestamp = DateTime.UtcNow });
            await _db.SaveChangesAsync();
 }

            return Page();
        }
    }
}
