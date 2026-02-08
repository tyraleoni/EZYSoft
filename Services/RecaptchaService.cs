using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace WebApplication1.Services
{
 public class RecaptchaOptions
 {
 public string SiteKey { get; set; }
 public string SecretKey { get; set; }
 public double MinimumScore { get; set; } =0.5;
 }

 public interface IRecaptchaService
 {
 Task<bool> VerifyTokenAsync(string token, string remoteIp = null);
 }

 public class RecaptchaService : IRecaptchaService
 {
 private readonly RecaptchaOptions _opts;
 private readonly HttpClient _client;

 public RecaptchaService(IOptions<RecaptchaOptions> opts, IHttpClientFactory factory)
 {
 _opts = opts.Value;
 _client = factory.CreateClient();
 }

 private class RecaptchaResponse
 {
 public bool success { get; set; }
 public double score { get; set; }
 public string action { get; set; }
 public DateTime challenge_ts { get; set; }
 public string hostname { get; set; }
 public string[] error_codes { get; set; }
 }

 public async Task<bool> VerifyTokenAsync(string token, string remoteIp = null)
 {
 if (string.IsNullOrEmpty(token)) return false;
 var values = new Dictionary<string, string>
 {
 {"secret", _opts.SecretKey},
 {"response", token}
 };
 if (!string.IsNullOrEmpty(remoteIp)) values.Add("remoteip", remoteIp);
 var resp = await _client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(values));
 if (!resp.IsSuccessStatusCode) return false;
 var body = await resp.Content.ReadFromJsonAsync<RecaptchaResponse>();
 if (body == null) return false;
 return body.success && body.score >= _opts.MinimumScore;
 }
 }
}