using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Model
{
 public class ApplicationUser : IdentityUser
 {
 // Additional profile fields can be added here
 public string? EncryptedData { get; set; }

 // New profile fields for registration
 public string? FirstName { get; set; }
 public string? LastName { get; set; }
 public string? Gender { get; set; }
 public DateTime? DateOfBirth { get; set; }

 // Encrypted NRIC
 public string? NRICEncrypted { get; set; }

 // Stored resume filename/path (relative)
 public string? ResumeFileName { get; set; }

 // Free-text biography or "Who am I" field (will be HTML-encoded when displayed)
 public string? WhoAmI { get; set; }
 }
}