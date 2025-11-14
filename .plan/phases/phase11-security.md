# Phase 11: Security Hardening 🔒

**Status**: Week 1 Complete ✅ (Week 2-3 Pending)  
**Priority**: HIGH 🟠  
**Estimated Time**: 8-10 hours total (Week 1: 2.5 hours ✅, Week 2-3: 5.5 hours remaining)  
**Prerequisite**: Phase 10 (Logging) - to monitor security events

**Week 1 Completed**: October 2024 ✅  
**Implementation**: Rate Limiting, Security Headers, CORS Hardening, Input Validation

---

## 🎯 WHY Security Hardening?

### The Problem

Your template is **publicly accessible** and **vulnerable** to common attacks:

| Attack Type          | Current Risk      | Impact                                     |
| -------------------- | ----------------- | ------------------------------------------ |
| **Rate Limiting**    | ❌ None           | Attackers can spam API (DDoS, brute force) |
| **Security Headers** | ❌ Missing        | Clickjacking, XSS, MIME attacks possible   |
| **Input Validation** | ⚠️ Partial        | Injection attacks possible                 |
| **CORS**             | ⚠️ Too permissive | Any origin can call API                    |
| **Secrets**          | ⚠️ Exposed        | Hardcoded in code/env vars                 |
| **Authentication**   | ⚠️ Basic          | No MFA, no password complexity             |
| **Authorization**    | ❌ None           | No role-based access control               |
| **Audit Logs**       | ❌ None           | Can't track who did what                   |

### Real-World Scenarios

**Scenario 1: Brute Force Attack**

```
Attacker: Tries 10,000 login attempts in 1 minute
Current: All attempts go through, costs you $$$ in Cognito charges
With Rate Limiting: Blocked after 5 failed attempts
```

**Scenario 2: XSS Attack**

```
Attacker: Injects <script>steal_cookies()</script> in product name
Current: Executes in other users' browsers
With Security Headers: CSP blocks inline scripts
```

**Scenario 3: Unauthorized Access**

```
User: Regular user tries to access admin endpoints
Current: All authenticated users can access everything
With RBAC: Only admins can access admin endpoints
```

---

## 📋 WHAT We'll Implement

### Priority 1: Critical (Must Have) 🔴

#### 1. Rate Limiting

**Why**: Prevent DDoS, brute force, API abuse  
**Where**: API Gateway (affects all services)  
**Implementation**: ASP.NET Core Rate Limiting middleware

**Limits**:

- Global: 100 requests/minute per IP
- Login endpoint: 5 attempts/15 minutes per IP
- Signup endpoint: 3 attempts/hour per IP

**Cost**: Free (built into .NET)

---

#### 2. Security Headers

**Why**: Prevent XSS, clickjacking, MIME attacks  
**Where**: All 3 services  
**Implementation**: Custom middleware

**Headers to add**:

```
X-Content-Type-Options: nosniff          # Prevent MIME sniffing
X-Frame-Options: DENY                    # Prevent clickjacking
X-XSS-Protection: 1; mode=block          # Enable XSS filter
Strict-Transport-Security: max-age=...   # Force HTTPS
Content-Security-Policy: ...             # Restrict resource loading
Referrer-Policy: strict-origin-when-...  # Control referrer info
Permissions-Policy: ...                  # Control browser features
```

**Cost**: Free (built into .NET)

---

#### 3. CORS Hardening

**Why**: Current `AllowAnyOrigin` is dangerous  
**Where**: All 3 services  
**Implementation**: Configure proper CORS policies

**Changes**:

```csharp
// BEFORE (insecure):
.AllowAnyOrigin()

// AFTER (secure):
.WithOrigins(
    "https://yourdomain.com",           // Production
    "https://staging.yourdomain.com",   // Staging
    "http://localhost:3000"             // Local dev only
)
```

**Cost**: Free

---

#### 4. Input Validation

**Why**: Prevent SQL injection, XSS, buffer overflow  
**Where**: All controllers (API Gateway, IdentityService, DataService)  
**Implementation**: Data Annotations + FluentValidation

**Example**:

```csharp
public class CreateProductDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9\s]+$")]  // Only alphanumeric + spaces
    public string Name { get; set; } = "";

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = "";
}
```

**Cost**: Free (FluentValidation package)

---

### Priority 2: Important (Should Have) 🟠

#### 5. Secrets Management (AWS Secrets Manager)

**Why**: Stop hardcoding secrets in code/env vars  
**Where**: Terraform + all services  
**Implementation**: AWS Secrets Manager + .NET integration

**What to secure**:

- Database passwords
- Cognito secrets
- API keys
- JWT signing keys

**Current**:

```csharp
ConnectionString = "...Password=postgres..."  // ❌ Hardcoded
```

**After**:

```csharp
var secret = await secretsManager.GetSecretValueAsync("db-password");
ConnectionString = $"...Password={secret}..."  // ✅ From Secrets Manager
```

**Cost**: $0.40/month per secret (4 secrets = $1.60/month)

---

#### 6. Role-Based Access Control (RBAC)

**Why**: Not all users should access all endpoints  
**Where**: All services  
**Implementation**: ASP.NET Core Authorization with Cognito Groups

**Roles**:

- `User` (default) - Can read products, manage own data
- `Admin` - Can create/edit/delete products
- `SuperAdmin` - Full access

**Example**:

```csharp
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<ActionResult> CreateProduct(...)
{
    // Only admins can reach here
}
```

**Cost**: Free (Cognito groups are free)

---

#### 7. Audit Logging

**Why**: Track who did what, when, and from where  
**Where**: All critical operations  
**Implementation**: Extend Serilog logging

**What to log**:

- Login attempts (success/failure)
- Password changes
- Admin actions (create/edit/delete)
- Data access (who viewed what)
- Configuration changes

**Example log**:

```json
{
  "Event": "ProductCreated",
  "UserId": "abc-123",
  "UserEmail": "admin@example.com",
  "IP": "1.2.3.4",
  "ProductId": "xyz-789",
  "Timestamp": "2025-10-23T12:34:56Z"
}
```

**Cost**: Free (uses existing CloudWatch)

---

### Priority 3: Nice to Have (Optional) 🟡

#### 8. Password Complexity Requirements

**Why**: Weak passwords = easy to crack  
**Where**: Cognito configuration  
**Implementation**: Update Cognito User Pool policies

**Requirements**:

- Minimum 12 characters
- At least 1 uppercase, 1 lowercase, 1 number, 1 special char
- No common passwords (password123, qwerty, etc.)

**Cost**: Free

---

#### 9. Multi-Factor Authentication (MFA)

**Why**: Extra layer of security (even if password is stolen)  
**Where**: Cognito  
**Implementation**: Enable TOTP MFA in Cognito

**Options**:

- SMS MFA (costs ~$0.00645/SMS)
- TOTP MFA (free - Google Authenticator, Authy)

**Cost**: Free (TOTP) or ~$5-10/month (SMS)

---

#### 10. WAF (Web Application Firewall)

**Why**: Block malicious traffic before it reaches your app  
**Where**: AWS WAF + CloudFront/ALB  
**Implementation**: Terraform WAF rules

**Protection against**:

- SQL injection patterns
- XSS patterns
- Known bad IPs (reputation lists)
- Bots
- Geographic blocking

**Cost**: $5/month (basic rules) + $0.60 per 1M requests

---

## 🛠️ Implementation Order

### Week 1: Critical Security (Free)

1. ✅ Rate Limiting (2 hours)
2. ✅ Security Headers (1 hour)
3. ✅ CORS Hardening (30 mins)
4. ✅ Input Validation (3 hours)

**Total**: 6.5 hours, $0/month

---

### Week 2: Secrets & RBAC (Low Cost)

5. ✅ Secrets Management (2 hours)
6. ✅ RBAC (3 hours)
7. ✅ Audit Logging (1 hour)

**Total**: 6 hours, ~$2/month

---

### Week 3: Optional Enhancements (Optional)

8. ⚠️ Password Complexity (30 mins)
9. ⚠️ MFA (1 hour)
10. ⚠️ WAF (2 hours)

**Total**: 3.5 hours, ~$5-10/month (if you add WAF)

---

## 📦 Packages Needed

### Backend (.NET)

```bash
# Rate Limiting (built into .NET 8)
# No package needed

# FluentValidation
dotnet add package FluentValidation.AspNetCore

# AWS Secrets Manager
dotnet add package AWSSDK.SecretsManager
```

### Frontend (React)

```bash
# DOMPurify (sanitize user input)
npm install dompurify @types/dompurify
```

---

## 🧪 Testing Security

### Rate Limiting Test

```bash
# Try 10 requests in 1 second (should get 429 after limit)
for i in {1..10}; do curl http://localhost:5002/api/v1/products; done
```

### Security Headers Test

```bash
# Check headers
curl -I http://localhost:5002/api/v1/health

# Should see:
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# etc.
```

### RBAC Test

```bash
# Try admin endpoint as regular user (should get 403 Forbidden)
curl -H "Authorization: Bearer <user_token>" \
     -X POST http://localhost:5002/api/v1/products
```

---

## 📊 Cost Summary

| Feature             | Monthly Cost         | Impact                        |
| ------------------- | -------------------- | ----------------------------- |
| Rate Limiting       | $0                   | High                          |
| Security Headers    | $0                   | High                          |
| CORS Hardening      | $0                   | High                          |
| Input Validation    | $0                   | High                          |
| Secrets Manager     | $1.60 (4 secrets)    | Medium                        |
| RBAC                | $0                   | Medium                        |
| Audit Logging       | $0 (uses CloudWatch) | Medium                        |
| Password Complexity | $0                   | Low                           |
| MFA (TOTP)          | $0                   | Low                           |
| WAF                 | $5-10                | Low (overkill for small apps) |

**Recommended**: Weeks 1 & 2 = **$1.60/month** for excellent security

---

## 🎓 Learning Objectives

By completing Phase 11, you'll understand:

1. ✅ **Rate Limiting**: How to prevent API abuse
2. ✅ **Security Headers**: Browser security mechanisms
3. ✅ **CORS**: Cross-origin security
4. ✅ **Input Validation**: Preventing injection attacks
5. ✅ **Secrets Management**: Secure credential storage
6. ✅ **RBAC**: Role-based access control
7. ✅ **Audit Logging**: Security event tracking
8. ✅ **Defense in Depth**: Multiple security layers

---

## 🚀 Ready to Start?

**Recommended approach**: Start with Week 1 (Critical Security - Free)

This gives you immediate protection against the most common attacks without any cost.

**Estimated time**: 6-7 hours (including learning and testing)

---

## 📚 References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [AWS Secrets Manager Pricing](https://aws.amazon.com/secrets-manager/pricing/)
- [Cognito MFA](https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-settings-mfa.html)

---

**Next Steps**:

1. Review this plan
2. Ask questions about any section
3. Start with Week 1 (Rate Limiting → Security Headers → CORS → Input Validation)
4. Test each feature as we implement it
5. Deploy to dev environment for real-world testing

Ready to harden your security? 🛡️





