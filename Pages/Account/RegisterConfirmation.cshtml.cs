using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages.Account
{
  public class RegisterConfirmationModel : PageModel
 {
    [FromQuery]
        public string Email { get; set; }

public void OnGet()
        {
        }
    }
}
