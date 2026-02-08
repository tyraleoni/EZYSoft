using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.ViewModels;
using WebApplication1.Model;
using WebApplication1.Services;
using System.Text.Encodings.Web;

namespace WebApplication1.Pages
{
    public class RegisterModel : PageModel
    {
        private UserManager<ApplicationUser> userManager { get; }
        private SignInManager<ApplicationUser> signInManager { get; }
        private readonly AuthDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IProtectionService _protector;
        private readonly IPasswordHasher<ApplicationUser> _hasher;
        private readonly IRecaptchaService _recaptcha;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        [BindProperty]
        public Register RModel { get; set; }

        [BindProperty]
        public IFormFile ResumeFile { get; set; }

        [BindProperty]
        public string RecaptchaToken { get; set; }

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AuthDbContext db, IWebHostEnvironment env, IProtectionService protector, IPasswordHasher<ApplicationUser> hasher, IRecaptchaService recaptcha, IEmailService emailService, IConfiguration config)
        {
            this.userManager = userManager; this.signInManager = signInManager; _db = db; _env = env; _protector = protector; _hasher = hasher; _recaptcha = recaptcha; _emailService = emailService; _config = config;
        }

        public void OnGet()
        {
            // reCAPTCHA configuration is now loaded via API endpoint
            // No need to set ViewData here
        }

        //Save data into the database
        public async Task<IActionResult> OnPostAsync()
        {
            // Remove noisy modelstate entries related to resume if present (keep resume optional)
            var keysToRemove = ModelState.Keys.Where(k => k != null && k.IndexOf("resume", StringComparison.OrdinalIgnoreCase) >=0).ToList();
            foreach (var k in keysToRemove)
            {
                ModelState.Remove(k);
            }

            // reCAPTCHA: skip verification in Development or when disabled in config to aid local testing
            var enabledInConfig = _config.GetValue<bool?>("Recaptcha:Enabled") ?? true;

            if (!_env.IsDevelopment() && enabledInConfig)
            {
                if (string.IsNullOrEmpty(RecaptchaToken))
                {
                    ModelState.AddModelError(string.Empty, "Please complete the reCAPTCHA verification.");
                    return Page();
                }

                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var ok = await _recaptcha.VerifyTokenAsync(RecaptchaToken, remoteIp);
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                    return Page();
                }
            }
            else
            {
                // reCAPTCHA is disabled for this environment/config — ensure ModelState doesn't contain a leftover error for the token
                if (ModelState.ContainsKey(nameof(RecaptchaToken)))
                {
                    ModelState.Remove(nameof(RecaptchaToken));
                }
                if (ModelState.ContainsKey("RecaptchaToken"))
                {
                    ModelState.Remove("RecaptchaToken");
                }
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existing = await userManager.FindByEmailAsync(RModel.Email);
                if (existing != null)
                {
                    ModelState.AddModelError(string.Empty, "Email already registered");
                    return Page();
                }

                // Validate resume file if uploaded
                string savedFileName = null;
                if (ResumeFile != null)
                {
                    var allowed = new[] { ".pdf", ".doc", ".docx" };
                    var ext = Path.GetExtension(ResumeFile.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext))
                    {
                        ModelState.AddModelError(string.Empty, "Resume must be a .pdf, .doc, or .docx file.");
                        return Page();
                    }

                    if (ResumeFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(string.Empty, "Resume file too large (max 5MB)");
                        return Page();
                    }

                    // Save file to wwwroot/uploads with a unique name
                    var uploads = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                    savedFileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploads, savedFileName);
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await ResumeFile.CopyToAsync(fs);
                    }
                }

                // Server-side sanitize WhoAmI (encode before storing)
                var encoder = HtmlEncoder.Default;
                var whoAmIEncoded = string.IsNullOrEmpty(RModel.WhoAmI) ? null : encoder.Encode(RModel.WhoAmI);

                // Create user
                var user = new ApplicationUser()
                {
                    UserName = RModel.Email,
                    Email = RModel.Email,
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    Gender = RModel.Gender,
                    DateOfBirth = RModel.DateOfBirth,
                    ResumeFileName = savedFileName,
                    WhoAmI = whoAmIEncoded
                };

                // Server-side password strength enforcement (12+ chars, upper, lower, digit, special)
                if (!IsStrongPassword(RModel.Password))
                {
                    ModelState.AddModelError(string.Empty, "Password does not meet complexity requirements (min12 chars, upper, lower, digit, special)");
                    return Page();
                }

                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
                    // Encrypt NRIC and update user record using UserManager to ensure proper tracking
                    user.NRICEncrypted = _protector.Protect(RModel.NRIC);
                    await userManager.UpdateAsync(user);

                    // Store password history (use the password hash set by Identity)
                    var ph = new PasswordHistory { UserId = user.Id, PasswordHash = user.PasswordHash, ChangedAt = DateTime.UtcNow };
                    _db.PasswordHistories.Add(ph);
                    _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "Register", Timestamp = DateTime.UtcNow });
                    await _db.SaveChangesAsync();

                    // Generate email confirmation token and send confirmation email
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { userId = user.Id, code = token }, protocol: Request.Scheme);
                    var emailBody = $@"
                    <html>
                    <body>
                    <h2>Welcome to Ace Job Agency!</h2>
                    <p>Please confirm your email address by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</p>
                    <p>Or copy this link: {HtmlEncoder.Default.Encode(callbackUrl)}</p>
                    <p>If you did not register, please ignore this email.</p>
                    </body>
                    </html>";

                    try
                    {
                        await _emailService.SendEmailAsync(user.Email, "Confirm your email - Ace Job Agency", emailBody);
                    }
                    catch (Exception ex)
                    {
                        // Log email error but don't fail registration
                        _db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "EmailSendFailure", Timestamp = DateTime.UtcNow });
                        await _db.SaveChangesAsync();
                    }

                    // Redirect to email confirmation pending page instead of auto-signing in
                    return RedirectToPage("/Account/RegisterConfirmation", new { email = user.Email });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

            }
            return Page();
        }

        private bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length <12) return false;
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }
}
