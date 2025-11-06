# Security Documentation

## Security Features

This application implements multiple layers of security to protect against common web vulnerabilities:

### 1. Authentication & Authorization
- **ASP.NET Core Identity** with strong password requirements
- Password minimum length: 12 characters
- Required: uppercase, lowercase, numbers, special characters
- **Role-based access control** (Admin role for management functions)
- Anti-forgery tokens on all state-changing operations

### 2. Input Validation & Sanitization

#### File Upload Security
- **Whitelist-based file type validation**
  - Images: .jpg, .jpeg, .png, .webp only
  - Audio: .mp3 only
- **File size limits**
  - Images: 10MB max per file
  - Audio: 15MB max per file
  - Total request: 100MB max
- **Image validation** - files are loaded to verify they're real images
- **Maximum dimensions** - 8000px to prevent DoS attacks
- **Secure filenames** - user input never used in file paths
- **Automatic cleanup** on validation failure

#### Form Input Validation
- Email validation
- Phone number format validation
- Message length limits (5000 characters max)
- SQL injection prevention via EF Core parameterized queries
- XSS prevention via automatic HTML encoding in Razor

### 3. Rate Limiting
- **Inquiry form**: 3 requests per 15 minutes per IP
- **IP detection**: Supports X-Forwarded-For header for reverse proxies
- In-memory cache-based rate limiting

### 4. HTTP Security Headers

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 0 (deprecated, CSP used instead)
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(), microphone=(), camera=(), usb=()
```

### 5. Content Security Policy (CSP)

Strict CSP policy to prevent XSS attacks:

```
default-src 'self';
script-src 'self' https://cdn.jsdelivr.net;
style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com;
img-src 'self' data: https:;
font-src 'self' https://fonts.gstatic.com;
media-src 'self';
connect-src 'self';
frame-ancestors 'none';
base-uri 'self';
form-action 'self';
```

### 6. HTTPS/TLS
- HTTPS redirection enforced
- HSTS (HTTP Strict Transport Security) enabled in production
- TLS 1.2+ only (configured in nginx)

### 7. Database Security
- **No credentials in source code** - all secrets via environment variables
- Parameterized queries via Entity Framework Core
- Connection pooling with limits
- Database user with minimum required privileges

### 8. Sensitive Data Protection
- **No hardcoded secrets** - all configuration via environment variables:
  - `DB_PASSWORD` - Database password
  - `SMTP_PASSWORD` - Email password
  - `ADMIN_PASSWORD` - Admin account password
  - `TRANSLATION_API_KEY` - Translation service key
- Secrets never logged or exposed in error messages
- Git ignore for sensitive files

### 9. Error Handling
- Generic error messages to users (no stack traces in production)
- Detailed logging for administrators
- Automatic transaction cleanup on failures

### 10. Session Security
- Secure cookie settings
- SameSite cookie attribute
- HTTP-only cookies for authentication
- 1-year expiration for culture cookies

## Security Checklist for Deployment

Before deploying to production, ensure:

- [ ] All environment variables are set (see `.env.example`)
- [ ] Database password is strong and unique
- [ ] Admin password is strong and changed from default
- [ ] SMTP credentials are properly configured
- [ ] HTTPS/SSL certificate is installed and valid
- [ ] Firewall is configured (ports 22, 80, 443 only)
- [ ] Server OS is up-to-date
- [ ] Database backups are automated
- [ ] `AllowedHosts` is set in `appsettings.Production.json`
- [ ] `ASPNETCORE_ENVIRONMENT=Production` is set
- [ ] Nginx is configured with security headers
- [ ] Rate limiting is tested and working

## Vulnerability Disclosure

If you discover a security vulnerability, please email:
**aristocratic-artwork-sell@proton.me**

Please include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

We will respond within 48 hours and work to fix the issue promptly.

## Security Updates

### Package Security

All packages are kept up-to-date to avoid known vulnerabilities:

- `SixLabors.ImageSharp`: 3.1.12 (latest, no known vulnerabilities)
- `Microsoft.AspNetCore.*`: 8.0.8 (latest for .NET 8)
- `Npgsql.EntityFrameworkCore.PostgreSQL`: 8.0.4
- `MailKit`: 4.8.0
- `QuestPDF`: 2024.10.3

### Update Policy

- Security patches applied within 7 days of disclosure
- Regular dependency updates monthly
- Automated vulnerability scanning with `dotnet list package --vulnerable`

## Secure Configuration Examples

### Environment Variables (.env)

```bash
# NEVER commit this file to git
# Use strong, unique passwords

DB_PASSWORD=Use_A_Very_Strong_Password_Here_123!
ADMIN_PASSWORD=Another_Strong_Admin_Password_456!
SMTP_PASSWORD=Your_SMTP_App_Specific_Password
```

### Nginx Security Configuration

```nginx
# Strong SSL configuration
ssl_protocols TLSv1.2 TLSv1.3;
ssl_prefer_server_ciphers on;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;

# Security headers
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
```

## Testing Security

### Manual Testing

```bash
# Test rate limiting
for i in {1..5}; do
  curl -X POST https://aristocraticartworksale.com/Inquiries/Create \
    -d "collectionId=1&message=test"
done
# Should return 429 on 4th request

# Test file upload validation
curl -X POST https://aristocraticartworksale.com/Admin/Collections/Create \
  -F "images=@malicious.php" \
  -H "Authorization: Bearer TOKEN"
# Should reject non-image files

# Test SQL injection (should be safe)
curl "https://aristocraticartworksale.com/collections/test' OR '1'='1"
# Should return 404, not database error
```

### Automated Security Scanning

```bash
# Check for vulnerable packages
dotnet list package --vulnerable

# OWASP Dependency Check
dependency-check --project AAS --scan ./src

# Security headers check
curl -I https://aristocraticartworksale.com | grep -i "x-frame-options\|x-content-type-options\|strict-transport-security"
```

## Additional Security Measures

1. **Regular security audits** - quarterly code reviews
2. **Penetration testing** - annual professional assessment
3. **Monitoring & logging** - all security events logged
4. **Incident response plan** - documented procedures for breaches
5. **Data backup** - daily encrypted backups with 30-day retention

## Compliance

This application follows security best practices from:
- OWASP Top 10 (2021)
- OWASP ASVS (Application Security Verification Standard)
- CWE/SANS Top 25 Most Dangerous Software Errors

## Security Contact

For security concerns: **aristocratic-artwork-sell@proton.me**

---

Last updated: 2025-01-05
