using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Model;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthDbContext _db;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProtectionService _protector;

        public bool SessionValid { get; set; }
        public int ActiveSessionsCount { get; set; }

        // Profile info for display
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? NRICDecrypted { get; set; }
        public string? NRICEncrypted { get; set; }

        public IndexModel(AuthDbContext db, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IProtectionService protector)
        {
            _db = db; _signInManager = signInManager; _userManager = userManager; _protector = protector;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            var sessionKey = HttpContext.Session.GetString("SessionKey");
            
            // If user is authenticated but no session key exists, create one
            if (string.IsNullOrEmpty(sessionKey))
            {
                // Create a new session for authenticated user
                sessionKey = Guid.NewGuid().ToString();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var ua = HttpContext.Request.Headers["User-Agent"].ToString();
                var sess = new UserSession 
                { 
                    UserId = user.Id, 
                    SessionKey = sessionKey, 
                    CreatedAt = DateTime.UtcNow, 
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30), 
                    IpAddress = ip, 
                    UserAgent = ua, 
                    IsActive = true 
                };
                _db.UserSessions.Add(sess);
                await _db.SaveChangesAsync();
                HttpContext.Session.SetString("SessionKey", sessionKey);
            }

            var s = _db.UserSessions.FirstOrDefault(x => x.SessionKey == sessionKey && x.UserId == user.Id && x.IsActive);
            if (s == null || s.ExpiresAt < DateTime.UtcNow)
            {
                // session invalid or expired
                if (s != null)
                {
                    s.IsActive = false; 
                    await _db.SaveChangesAsync();
                }
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Account/Logout");
            }

            // session valid, extend sliding expiration
            s.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
            await _db.SaveChangesAsync();
            HttpContext.Session.SetString("SessionKey", s.SessionKey);

            // Count active session for user
            ActiveSessionsCount = _db.UserSessions.Count(x => x.UserId == user.Id && x.IsActive);
            SessionValid = true;

            // Populate profile display fields
            DisplayName = string.IsNullOrEmpty(user.FirstName) && string.IsNullOrEmpty(user.LastName) ? user.UserName : $"{user.FirstName} {user.LastName}";
            Email = user.Email;
            NRICEncrypted = user.NRICEncrypted;
            if (!string.IsNullOrEmpty(user.NRICEncrypted))
            {
                try
                {
                    var raw = _protector.Unprotect(user.NRICEncrypted);
                    // mask NRIC for UI by showing first and last char
                    if (!string.IsNullOrEmpty(raw) && raw.Length >2)
                    {
                        NRICDecrypted = raw.Substring(0,1) + new string('*', Math.Max(0, raw.Length -2)) + raw.Substring(raw.Length -1);
                    }
                    else
                    {
                        NRICDecrypted = raw;
                    }
                }
                catch
                {
                    NRICDecrypted = null;
                }
            }

            return Page();
        }
    }
}
