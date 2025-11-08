# Security Configuration Guidelines

## IMPORTANT: Sensitive Data Management

**NEVER commit sensitive credentials to source control!**

### Configuration Priority

The application uses the following priority for configuration (highest to lowest):
1. **Environment Variables** (RECOMMENDED for production)
2. appsettings.json
3. appsettings.Development.json

### Required Environment Variables for Production

Set these environment variables in your production environment:

#### Database
- `DATABASE_URL` - Full PostgreSQL connection string, OR:
- `DB_HOST` - Database host
- `DB_PORT` - Database port
- `DB_NAME` - Database name
- `DB_USER` - Database username
- `DB_PASSWORD` - Database password (**REQUIRED**)

#### Email
- `EMAIL_SMTP_HOST` - SMTP server hostname
- `EMAIL_SMTP_PORT` - SMTP port (default: 587)
- `EMAIL_USE_STARTTLS` - Use STARTTLS (true/false)
- `EMAIL_USERNAME` - SMTP username
- `EMAIL_PASSWORD` - SMTP password
- `EMAIL_FROM` - From email address
- `EMAIL_TO` - To email address

#### Admin Account
- `ADMIN_EMAIL` - Admin account email
- `ADMIN_PASSWORD` - Admin account password (min 12 chars, requires uppercase & special chars)

#### Translation (Optional)
- `TRANSLATION_ENABLED` - Enable translation service (true/false)
- `TRANSLATION_ENDPOINT` - Translation API endpoint
- `TRANSLATION_API_KEY` - Translation API key

### Development vs Production

#### Development (appsettings.Development.json)
- Contains test credentials for local development
- Uses localhost SMTP (e.g., MailHog on port 1025)
- Safe to commit (no real credentials)

#### Production (Environment Variables)
- NEVER use appsettings.json for production secrets
- Use environment variables, Azure Key Vault, AWS Secrets Manager, etc.
- Ensure appsettings.json contains EMPTY strings for all secrets

### Securing appsettings.json

The default appsettings.json should look like this:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 587,
    "UseStartTls": true,
    "Username": "",
    "Password": "",
    "From": "",
    "To": ""
  },
  "Admin": {
    "Email": "",
    "Password": ""
  },
  "Translation": {
    "Provider": "LibreTranslate",
    "Endpoint": "",
    "ApiKey": "",
    "Enabled": false
  }
}
```

### Best Practices

1. **Use .gitignore** - Ensure appsettings.*.json files with secrets are ignored
2. **Create appsettings.example.json** - Template without secrets for documentation
3. **Use Secret Managers** - Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
4. **Rotate Credentials** - Change passwords and API keys regularly
5. **Principle of Least Privilege** - Use database accounts with minimum required permissions
6. **Monitor Access** - Log and monitor access to secrets
7. **Encrypt Backups** - Ensure database backups are encrypted

### Docker/Kubernetes

Use environment variables in your deployment:

```yaml
# docker-compose.yml example
environment:
  - DB_PASSWORD=${DB_PASSWORD}
  - EMAIL_PASSWORD=${EMAIL_PASSWORD}
  - ADMIN_PASSWORD=${ADMIN_PASSWORD}
```

```yaml
# Kubernetes secret example
apiVersion: v1
kind: Secret
metadata:
  name: aas-secrets
type: Opaque
data:
  db-password: <base64-encoded>
  email-password: <base64-encoded>
  admin-password: <base64-encoded>
```

### Verification

To verify your configuration is secure:

1. Check appsettings.json contains NO real passwords
2. Check .gitignore includes appsettings.*.json (except .Development.json)
3. Verify environment variables are set in production
4. Run security scan on repository for exposed secrets
5. Review git history for accidentally committed secrets

### If Secrets Were Committed

1. **Rotate ALL exposed credentials immediately**
2. Use `git filter-branch` or BFG Repo-Cleaner to remove from history
3. Force push cleaned repository
4. Notify team and security personnel
5. Monitor for unauthorized access

### Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/)
- [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/)
- [HashiCorp Vault](https://www.vaultproject.io/)
