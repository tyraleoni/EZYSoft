using System.Net;
using System.Net.Mail;

namespace WebApplication1.Services
{
 public interface IEmailService
 {
 Task SendEmailAsync(string to, string subject, string body);
 }

 public class SmtpEmailService : IEmailService
 {
 private readonly IConfiguration _config;
 public SmtpEmailService(IConfiguration config) { _config = config; }

 public async Task SendEmailAsync(string to, string subject, string body)
 {
 var host = _config["Smtp:Host"];
 var port = _config.GetValue<int>("Smtp:Port");
 var user = _config["Smtp:User"];
 var pass = _config["Smtp:Password"];
 var from = _config["Smtp:From"];
 var enableSsl = _config.GetValue<bool>("Smtp:EnableSsl");

 var msg = new MailMessage(from, to, subject, body);
 msg.IsBodyHtml = true;
 using var client = new SmtpClient(host, port)
 {
 Credentials = new NetworkCredential(user, pass),
 EnableSsl = enableSsl
 };
 await client.SendMailAsync(msg);
 }
 }
}