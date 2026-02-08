using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
 public class PasswordHistory
 {
 [Key]
 public int Id { get; set; }
 public string UserId { get; set; }
 public string PasswordHash { get; set; }
 public DateTime ChangedAt { get; set; }
 }
}