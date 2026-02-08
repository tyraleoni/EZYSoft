using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.ViewModels
{
    public class Register
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters long")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation password does not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(100)]
        [RegularExpression("^[a-zA-Z\\s'-]{1,100}$", ErrorMessage = "First name contains invalid characters")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        [RegularExpression("^[a-zA-Z\\s'-]{1,100}$", ErrorMessage = "Last name contains invalid characters")]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "NRIC must be between 3 and 20 characters")]
        [RegularExpression("^[a-zA-Z0-9-]+$", ErrorMessage = "NRIC contains invalid characters")]
        public string NRIC { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        // Resume will be handled via page model's ResumeFile to avoid duplicate binding

        [StringLength(2000)]
        public string WhoAmI { get; set; }
    }
}
