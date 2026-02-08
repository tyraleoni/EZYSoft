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
 private readonly ILogger<SmtpEmailService> _logger;

 public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) 
 { 
  _config = config;
   _logger = logger;
 }

 public async Task SendEmailAsync(string to, string subject, string body)
 {
 try
 {
  var host = _config["Smtp:Host"];
   var port = _config.GetValue<int>("Smtp:Port");
    var user = _config["Smtp:User"];
     var pass = _config["Smtp:Password"];
      var from = _config["Smtp:From"];
       var enableSsl = _config.GetValue<bool>("Smtp:EnableSsl");

  // Validate SMTP configuration
  if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
    _logger.LogError("SMTP configuration is incomplete. Host: {Host}, User: {User}, Port: {Port}", 
    host, user, port);
       throw new InvalidOperationException("SMTP configuration is not properly set.");
            }

            _logger.LogInformation("Sending email to {To} from {From} via {Host}:{Port}", 
      to, from, host, port);

            var msg = new MailMessage(from, to, subject, body);
 msg.IsBodyHtml = true;
            using var client = new SmtpClient(host, port)
            {
     Credentials = new NetworkCredential(user, pass),
    EnableSsl = enableSsl,
   Timeout = 10000 // 10 seconds timeout
            };
  await client.SendMailAsync(msg);
      
     _logger.LogInformation("Email successfully sent to {To}", to);
        }
        catch (SmtpException ex)
 {
      _logger.LogError(ex, "SMTP error sending email to {To}. Status: {StatusCode}", 
           to, ex.StatusCode);
            throw;
  }
      catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
      }
 }
 }
}