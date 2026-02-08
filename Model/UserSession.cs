using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
 public class UserSession
 {
 [Key]
 public int Id { get; set; }
 public string UserId { get; set; }
 public string SessionKey { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime ExpiresAt { get; set; }
 public string IpAddress { get; set; }
 public string UserAgent { get; set; }
 public bool IsActive { get; set; }
 }
}