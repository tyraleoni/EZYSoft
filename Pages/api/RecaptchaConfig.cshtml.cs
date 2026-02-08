using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;

namespace WebApplication1.Pages.Api
{
    public class RecaptchaConfigModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public RecaptchaConfigModel(IConfiguration config, IWebHostEnvironment env)
        {
  _config = config;
   _env = env;
        }

 public IActionResult OnGet()
        {
            var siteKey = _config["Recaptcha:SiteKey"];
 var enabledInConfig = _config.GetValue<bool?>("Recaptcha:Enabled") ?? true;
 
  // Use the configuration setting directly, don't override based on environment
    return new JsonResult(new
            {
           siteKey = enabledInConfig ? siteKey : null,
      enabled = enabledInConfig,
        minimumScore = _config.GetValue<double>("Recaptcha:MinimumScore", 0.5)
 });
        }
    }
}
