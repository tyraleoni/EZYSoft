using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages.StatusCode
{
    public class IndexModel : PageModel
    {
        public int Code { get; set; }
        public string? Message { get; set; }

        public void OnGet(int code)
        {
            Code = code;
            switch (code)
            {
                case 404:
                    Message = "Page not found.";
                    break;
                case 403:
                    Message = "Access denied.";
                    break;
                default:
                    Message = "An error occurred.";
                    break;
            }
        }
    }
}
