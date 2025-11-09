# 🛡️ Security Checklist - Produkční Nasazení

## Kontrolní seznam před spuštěním do produkce

---

## ✅ Před Nasazením

### Konfigurace

- [ ] Všechny environment variables v `.env.production` jsou vyplněny
- [ ] `.env.production` je v `.gitignore`
- [ ] Database password má minimálně 16 znaků
- [ ] Admin password splňuje požadavky (min 12 znaků, uppercase, lowercase, čísla, speciální znaky)
- [ ] `appsettings.json` NEOBSAHUJE žádné real credentials
- [ ] `appsettings.Production.json` má prázdné hodnoty pro všechny secrets
- [ ] `AllowedHosts` v `appsettings.Production.json` obsahuje pouze produkční doménu

### SSL/TLS

- [ ] SSL certifikáty jsou nainstalovány v `nginx/ssl/`
- [ ] SSL certifikáty jsou validní a neexpirované
- [ ] Certifikáty pokrývají všechny domény (včetně www)
- [ ] Automatická obnova certifikátů je nakonfigurována (cron)
- [ ] HTTPS redirect je funkční (HTTP -> HTTPS)
- [ ] HSTS header je aktivní

### Docker & Infrastructure

- [ ] Docker images jsou buildovány pro production
- [ ] Non-root user je použit v Docker containeru
- [ ] Health checks jsou nakonfigurovány pro všechny služby
- [ ] Resource limits jsou nastaveny (CPU, RAM)
- [ ] Volumes pro data persistence jsou vytvořeny
- [ ] Container auto-restart je nakonfigurován (`restart: unless-stopped`)

### Network & Firewall

- [ ] Firewall povoluje pouze porty 22, 80, 443
- [ ] SSH je zabezpečen (pouze key-based auth, ne password)
- [ ] Database port (5432) NENÍ veřejně přístupný
- [ ] Pouze Nginx je vystavený na internetu
- [ ] Rate limiting je aktivní v Nginx

---

## ✅ Po Nasazení

### Application Security

- [ ] Admin účet je vytvořen a funkční
- [ ] Výchozí/testovací účty jsou smazány
- [ ] CSRF protection funguje (testováno)
- [ ] XSS protection je aktivní (HTML je escapován)
- [ ] SQL injection prevence (Entity Framework)
- [ ] File upload validation funguje
- [ ] Email notifications fungují
- [ ] Error pages nezobrazují stack traces (pouze v dev)
- [ ] Antiforgery tokens fungují správně

### Monitoring & Logging

- [ ] Application logs jsou dostupné
- [ ] Error logging je aktivní
- [ ] Nginx access/error logy jsou dostupné
- [ ] Database logy jsou dostupné
- [ ] Uptime monitoring je nastaven (e.g., UptimeRobot)
- [ ] Disk space alerting
- [ ] CPU/RAM monitoring

### Backup & Recovery

- [ ] Automatická záloha databáze je nakonfigurována
- [ ] Backup retention policy je nastavena
- [ ] Backup úspěšnosti jsou monitorovány
- [ ] Disaster recovery plán je zdokumentován
- [ ] Restore databáze byl otestován
- [ ] Backup uploaded souborů je nastaven

### Performance

- [ ] Gzip compression je aktivní
- [ ] Static file caching funguje
- [ ] CDN je nakonfigurován (pokud používáte)
- [ ] Database indexy jsou optimalizovány
- [ ] Connection pooling je aktivní
- [ ] Response time je < 2s

---

## 🔒 Security Headers

Ověřte, že následující HTTP security headers jsou nastaveny:

```bash
curl -I https://aristocraticartworksale.com
```

Očekávané headers:

- [ ] `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`
- [ ] `X-Frame-Options: DENY`
- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-XSS-Protection: 0`
- [ ] `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] `Permissions-Policy: geolocation=(), microphone=(), camera=(), usb=()`
- [ ] `Content-Security-Policy` je nastaven

---

## 🔐 Credentials Management

### Co NESMÍ být v Git repository:

- [ ] `.env.production`
- [ ] SSL private keys
- [ ] Database passwords
- [ ] Email passwords
- [ ] API keys
- [ ] Admin credentials

### Co MUSÍ být v `.gitignore`:

```
.env.production
.env.*.local
nginx/ssl/*.pem
nginx/ssl/*.key
backups/
*.sql
*.dump
```

Ověřte:
```bash
git status --ignored
```

---

## 🧪 Security Testing

### Manual Testing

- [ ] Zkuste XSS útok v collection description
- [ ] Zkuste SQL injection v search polích
- [ ] Zkuste nahrát nevalidní soubor (např. .exe)
- [ ] Zkuste brute force login (mělo by být rate limitováno)
- [ ] Zkuste přístup k admin oblasti bez přihlášení
- [ ] Zkuste CSRF útok na admin actions

### Automated Testing

- [ ] Spusťte SSL test: https://www.ssllabs.com/ssltest/
- [ ] Spusťte security headers test: https://securityheaders.com/
- [ ] Zkontrolujte známé vulnerabilities: `docker scan aas-web-prod`

### Penetration Testing (doporučeno)

- [ ] OWASP ZAP scan
- [ ] Nikto web scanner
- [ ] Professional pentest (pokud máte budget)

---

## 📊 Compliance

### GDPR

- [ ] Privacy policy je dostupná
- [ ] Cookie consent (pokud používáte cookies kromě nutných)
- [ ] Data retention policy
- [ ] Postup pro "Right to be forgotten"

### Data Protection

- [ ] Databáze backupy jsou šifrovány
- [ ] HTTPS je vynuceno všude
- [ ] Sensitivní data v logu jsou maskována
- [ ] Personal data jsou identifikována a chráněna

---

## 🚨 Incident Response

### V případě bezpečnostního incidentu:

1. **Okamžitě:**
   - [ ] Zastavit aplikaci: `docker-compose down`
   - [ ] Izolovat server od internetu
   - [ ] Informovat administrátory

2. **Do 1 hodiny:**
   - [ ] Analyzovat logy
   - [ ] Identifikovat rozsah útoku
   - [ ] Rotovat všechny credentials
   - [ ] Aplikovat security patches

3. **Do 24 hodin:**
   - [ ] Obnovit ze zálohy (pokud nutné)
   - [ ] Informovat uživatele (pokud data leak)
   - [ ] Dokumentovat incident
   - [ ] Implementovat dodatečná opatření

### Emergency Contacts

```
Admin: [VÁŠ EMAIL]
Hosting provider: [SUPPORT EMAIL]
Security team: [SECURITY EMAIL]
```

---

## 📅 Pravidelná Údržba

### Denně

- [ ] Zkontrolovat Docker container health
- [ ] Zkontrolovat disk space
- [ ] Zkontrolovat error logy

### Týdně

- [ ] Zkontrolovat backup úspěšnost
- [ ] Zkontrolovat security logs
- [ ] Zkontrolovat performance metrics
- [ ] Update Docker images (pokud jsou security patches)

### Měsíčně

- [ ] Update operačního systému
- [ ] Update Docker a Docker Compose
- [ ] Update .NET runtime (pokud minor version)
- [ ] Rotovat log soubory
- [ ] Test disaster recovery

### Čtvrtletně

- [ ] Update .NET aplikace (major version)
- [ ] Security audit
- [ ] Penetration testing
- [ ] Review access logs
- [ ] Update dokumentace

---

## 🎯 Security Scoring

Po dokončení všech checklist items, vaše aplikace by měla mít:

- **SSL Labs Grade:** A nebo A+
- **Security Headers Grade:** A nebo A+
- **OWASP Top 10:** Všechny kategorie zabezpečeny
- **Uptime:** 99.9%+
- **MTTR (Mean Time To Recovery):** < 1 hodina

---

## 📝 Security Audit Log

Dokumentujte všechny security události:

| Datum | Událost | Akce | Provedl |
|-------|---------|------|---------|
| 2025-01-09 | Initial deployment | Production deployment completed | Admin |
| | | | |

---

## 📞 Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [Mozilla Security Guidelines](https://infosec.mozilla.org/guidelines/web_security)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

---

**Důležité:** Tento checklist by měl být projít před KAŽDÝM produkčním nasazením!

*Poslední update: 2025-01-09*
*Verze: 1.0*
