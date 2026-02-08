# ?? SECURITY QUICK REFERENCE GUIDE

## Ace Job Agency / EZYSoft Security Implementation

---

## QUICK CHECKLIST VERIFICATION

| Item | Status | Location |
|------|--------|----------|
| ? User Registration | Implemented | `Pages/Register.cshtml.cs` |
| ? Email Verification | Required | `Pages/Account/VerifyEmailRegistration.cshtml` |
| ? Strong Passwords | Enforced | `Program.cs` + `Pages/Register.cshtml.cs` |
| ? Password Hashing | Automatic | ASP.NET Identity |
| ? NRIC Encryption | Yes | `Services/ProtectionService.cs` |
| ? File Upload Validation | Strict | `Pages/Register.cshtml.cs` (5MB, PDF/DOC/DOCX) |
| ? Secure Sessions | Implemented | `Pages/Account/Login.cshtml.cs` |
| ? Session Timeout | 20 min idle | `Program.cs` |
| ? Login Security | Multi-factor | Email + Password + 2FA |
| ? Logout Cleanup | Complete | `Pages/Account/Logout.cshtml.cs` |
| ? Account Lockout | 3 attempts/15 min | `Program.cs` Identity config |
| ? Audit Logging | Comprehensive | `Model/AuditLog.cs` (40+ events) |
| ? 2FA (TOTP) | Mandatory | `Pages/Account/Enable2FA.cshtml.cs` |
| ? Password Reset | Email-based | `Pages/Account/ResetPassword.cshtml.cs` |
| ? Password History | 2-year tracking | `Model/PasswordHistory.cs` |
| ? Max Password Age | 90 days | `appsettings.json` + Middleware |
| ? reCAPTCHA | v3 Bot detection | `Services/RecaptchaService.cs` |
| ? CSRF Protection | Antiforgery tokens | All forms |
| ? XSS Prevention | Encoding | `HtmlEncoder.Default.Encode()` |
| ? SQL Injection | EF Core parameterized | No raw SQL |
| ? HTTPS | Enforced | `Program.cs` |
| ? Error Handling | Safe messages | Generic responses |
| ? Custom Error Pages | 404, 403, etc. | `Pages/StatusCode/` |

---

## KEY SECURITY FILES

```
?? Project Structure - Security Components

Pages/
??? Account/
?   ??? Login.cshtml.cs? Authentication
?   ??? Register.cshtml.cs        ? Registration validation
?   ??? Enable2FA.cshtml.cs       ? 2FA setup
? ??? Verify2FA.cshtml.cs       ? 2FA verification
?   ??? ChangePassword.cshtml.cs  ? Password policy enforcement
?   ??? ForgotPassword.cshtml.cs  ? Password reset request
?   ??? ResetPassword.cshtml.cs   ? Password reset execution
?   ??? Logout.cshtml.cs          ? Secure logout
?   ??? VerifyEmailRegistration   ? Email verification
??? Index.cshtml.cs     ? Session validation
??? StatusCode/Index.cshtml.cs    ? Error handling

Services/
??? ProtectionService.cs          ? Data encryption (NRIC)
??? EmailService.cs   ? Secure email (with error handling)
??? RecaptchaService.cs           ? Bot detection

Model/
??? ApplicationUser.cs ? User properties
??? AuditLog.cs          ? Audit trail
??? PasswordHistory.cs        ? Password tracking
??? UserSession.cs       ? Session management
??? AuthDbContext.cs     ? Database context

Program.cs      ? Security configuration

appsettings.json     ? Password policy settings
```

---

## SECURITY CONFIGURATION REFERENCE

### Password Policy (appsettings.json)
```json
"PasswordPolicy": {
 "MinChangeMinutes": 5,      // Min time between changes
    "MaxAgeDays": 90       // Max password age
}
```

### Identity Options (Program.cs)
```csharp
options.Password.RequiredLength = 12;
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;

options.Lockout.MaxFailedAccessAttempts = 3;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

options.User.RequireUniqueEmail = true;
```

### Session Configuration (Program.cs)
```csharp
options.IdleTimeout = TimeSpan.FromMinutes(20);
options.Cookie.HttpOnly = true;
options.Cookie.IsEssential = true;
```

### SMTP Configuration (appsettings.Development.json)
```json
"Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "your-email@gmail.com",
    "Password": "app-specific-password",
    "From": "your-email@gmail.com",
    "EnableSsl": true
}
```

---

## COMMON SECURITY OPERATIONS

### Check Failed Login Attempts
```sql
SELECT TOP 100 UserId, Action, Timestamp 
FROM dbo.AuditLogs 
WHERE Action LIKE '%LoginFailure%' 
ORDER BY Timestamp DESC;
```

### View All 2FA Events
```sql
SELECT UserId, Action, Timestamp 
FROM dbo.AuditLogs 
WHERE Action LIKE '%2FA%' 
ORDER BY Timestamp DESC;
```

### Check Password Changes
```sql
SELECT UserId, ChangedAt 
FROM dbo.PasswordHistories 
ORDER BY ChangedAt DESC;
```

### View Active Sessions
```sql
SELECT UserId, SessionKey, CreatedAt, ExpiresAt, IpAddress, IsActive
FROM dbo.UserSessions 
WHERE IsActive = 1;
```

### Unlock User Account (if locked)
```sql
UPDATE dbo.AspNetUsers 
SET LockoutEnd = NULL 
WHERE Email = 'user@example.com';
```

### Force Password Reset
```sql
DELETE FROM dbo.PasswordHistories 
WHERE UserId = @UserId;

UPDATE dbo.AspNetUsers 
SET PasswordHash = NULL 
WHERE Id = @UserId;

-- User will need to use Forgot Password
```

---

## TESTING SECURITY FEATURES

### Test 1FA Authentication
1. Go to `/Account/Login`
2. Enter credentials
3. Should redirect to Enable2FA or Verify2FA
4. ? Check: 2FA is mandatory

### Test Password Strength
1. Go to `/Register`
2. Try passwords:
   - "short" ? Rejected
   - "OnlyLetters123!" ? Rejected (too short)
   - "StrongPass123!" ? Accepted
3. ? Check: Password validation working

### Test Account Lockout
1. Go to `/Account/Login`
2. Enter wrong password 3 times
3. Should show: "Account locked out..."
4. Wait 15 minutes or manually unlock in SQL
5. ? Check: Lockout mechanism working

### Test Audit Logging
1. Perform various actions (login, 2FA, password change)
2. Query: `SELECT * FROM dbo.AuditLogs ORDER BY Timestamp DESC`
3. ? Check: All actions logged

### Test CSRF Protection
1. Disable JavaScript or remove token
2. Try submitting form
3. Should fail with invalid token error
4. ? Check: CSRF protection active

### Test Input Validation
1. Go to `/Register`
2. Try entering HTML tags or SQL in fields
3. Should be rejected or encoded
4. ? Check: Input sanitization working

### Test NRIC Encryption
1. Register user with NRIC
2. Query database: `SELECT NRICEncrypted FROM dbo.AspNetUsers`
3. Should see encrypted value (not plain text)
4. Go to profile page
5. NRIC should be masked: "X****X"
6. ? Check: Encryption working

---

## MONITORING CHECKLIST

### Daily
- [ ] Check for failed login attempts in AuditLogs
- [ ] Verify no unusual 2FA failures
- [ ] Monitor for any errors in email service

### Weekly
- [ ] Review password change patterns
- [ ] Check for account lockouts
- [ ] Verify session timeouts working
- [ ] Review error logs

### Monthly
- [ ] Generate security report
- [ ] Review all audit logs
- [ ] Check for suspicious patterns
- [ ] Verify backups working

### Quarterly
- [ ] Full security audit
- [ ] Penetration testing
- [ ] Update security documentation
- [ ] Review and update security policies

---

## INCIDENT RESPONSE QUICK GUIDE

### Suspected Account Compromise
```
1. Unlock account if locked: UPDATE AspNetUsers SET LockoutEnd = NULL...
2. Force password reset: DELETE FROM PasswordHistories...
3. Check AuditLogs for unauthorized access
4. Review all sessions from suspicious IPs
5. Notify user immediately
6. Monitor account activity
```

### Suspected Data Breach
```
1. Check AuditLogs for unauthorized data access
2. Review which users/data were accessed
3. Isolate affected systems
4. Check backups for integrity
5. Engage legal/compliance team
6. Notify affected users within 48 hours
```

### Performance Issues from Logging
```
1. Archive old AuditLogs (older than 1 year)
2. Create indexes on commonly queried columns
3. Consider log aggregation service
4. Implement log retention policy
```

---

## SECURITY HARDENING CHECKLIST

### Immediate (Before Production)
- [x] HTTPS enabled
- [x] Password policy configured
- [x] 2FA implemented
- [x] CSRF tokens on all forms
- [x] Input validation implemented
- [x] Encryption for sensitive data
- [x] Error messages safe
- [x] Audit logging active

### Before Going Live
- [ ] Database backups configured
- [ ] Security headers added
- [ ] Penetration testing completed
- [ ] Security incident response plan documented
- [ ] User security training created
- [ ] Admin procedures documented
- [ ] Monitoring alerts configured
- [ ] Disaster recovery plan tested

### Ongoing
- [ ] Weekly security review
- [ ] Monthly audit log analysis
- [ ] Quarterly penetration testing
- [ ] Annual security audit
- [ ] Dependency updates
- [ ] Security training refresher

---

## CONTACT & ESCALATION

**Security Issues Found?**
1. Do NOT commit to main branch
2. Create security issue (private)
3. Notify security team immediately
4. Do NOT disclose in public issues

**Questions about implementation?**
- Review SECURITY_AUDIT_REPORT.md
- Check SECURITY_RECOMMENDATIONS.md
- Examine source code comments
- Consult security team

---

**Last Updated:** 2025
**Status:** ? COMPLETE
**Review Date:** Every 90 days recommended
