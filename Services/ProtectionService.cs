using Microsoft.AspNetCore.DataProtection;

namespace WebApplication1.Services
{
 public interface IProtectionService
 {
 string Protect(string plaintext);
 string Unprotect(string protectedText);
 }

 public class ProtectionService : IProtectionService
 {
 private readonly IDataProtector _protector;

 public ProtectionService(IDataProtectionProvider provider)
 {
 _protector = provider.CreateProtector("AceJobAgency.NRIC.Protect");
 }

 public string Protect(string plaintext)
 {
 if (string.IsNullOrEmpty(plaintext)) return null;
 return _protector.Protect(plaintext);
 }

 public string Unprotect(string protectedText)
 {
 if (string.IsNullOrEmpty(protectedText)) return null;
 try
 {
 return _protector.Unprotect(protectedText);
 }
 catch
 {
 return null;
 }
 }
 }
}