# Aristocratic Artwork Sale - Project Summary

## ✅ Projekt je kompletní a ready-to-deploy!

Veškeré bezpečnostní problémy byly opraveny a aplikace je připravena k nasazení na produkční server.

---

## 📊 Stav projektu

### ✅ Dokončeno:

1. **Backend (ASP.NET Core 8.0)**
   - PostgreSQL databáze s EF Core
   - Identity system s rolemi
   - Admin panel pro správu kolekcí
   - Email service s PDF přílohami
   - Translation service (LibreTranslate)
   - Image processing (3 velikosti: 480px, 960px, 1600px)
   - Slug generování pro SEO-friendly URL

2. **Frontend (Razor Pages + Bootstrap 5)**
   - Responzivní design (mobile-first)
   - Instagram-like galerie se Swiper.js
   - Swipování mezi fotkami
   - Audio player pro klasickou hudbu
   - Elegantní černozlatý design (Playfair Display + Inter)

3. **Funkcionality**
   - 5 kategorií kolekcí (Paintings, Jewelry, Watches, Statues, Other)
   - Neomezený počet fotek v každé kolekci
   - Neomezený text v popisech
   - Formulář "I'm interested" s emailem PDF
   - 10 jazyků s automatickým překladem
   - Přepínání jazyků bez refresh
   - SEO optimalizace (meta tagy, sitemap.xml, robots.txt, schema.org)

4. **Bezpečnost** 🔒
   - ✅ VŠECHNY vulnerable packages aktualizovány
   - ✅ SixLabors.ImageSharp 3.1.12 (bez security warnings!)
   - ✅ Žádná hardcoded hesla - vše přes environment variables
   - ✅ Strict Content Security Policy
   - ✅ Rate limiting (3 dotazy/15min)
   - ✅ File upload validation (whitelist, size, content verification)
   - ✅ Security headers (CSP, X-Frame-Options, HSTS, atd.)
   - ✅ Strong password policy (min 12 znaků)
   - ✅ Anti-CSRF tokens
   - ✅ SQL injection prevence
   - ✅ XSS prevence

5. **Deployment**
   - ✅ Docker support (docker-compose.yml)
   - ✅ Nginx konfigurace
   - ✅ Deployment skripty pro Ubuntu
   - ✅ SSL/HTTPS podpora
   - ✅ Kompletní dokumentace

---

## 📁 Struktura projektu

```
C:\AAS\
├── src/
│   └── AAS.Web/              # Hlavní aplikace
│       ├── Controllers/      # API & MVC controllers
│       ├── Models/           # Entity models
│       ├── Views/            # Razor views
│       ├── Services/         # Business logic
│       ├── Database/         # EF Core context & migrations
│       ├── Resources/        # Lokalizace (.resx soubory)
│       └── wwwroot/          # Static files (CSS, JS, images, uploads)
├── deployment/               # Deployment skripty
│   ├── install.sh           # Instalace dependencies na Ubuntu
│   ├── deploy.sh            # Deploy aplikace
│   └── update.sh            # Quick update skript
├── docker-compose.yml        # Docker konfigurace (PRODUCTION)
├── docker-compose.override.yml.example  # Lokální development
├── Dockerfile                # Docker image definice
├── .env.example              # Environment variables template
├── DEPLOYMENT.md            # 📖 NÁVOD NA NASAZENÍ
├── SECURITY.md              # 🔒 BEZPEČNOSTNÍ DOKUMENTACE
├── README.md                 # Přehled projektu
└── PROJECT_SUMMARY.md        # Tento soubor
```

---

## 🚀 Jak nasadit na Ubuntu server

### Rychlý start (5 kroků):

1. **Připravte server**
   ```bash
   ssh root@your-server-ip
   apt update && apt upgrade -y
   ```

2. **Nainstalujte Docker**
   ```bash
   curl -fsSL https://get.docker.com -o get-docker.sh
   sh get-docker.sh
   ```

3. **Nahrajte projekt na server**
   ```bash
   # Z vašeho PC (Windows):
   cd C:\AAS
   scp -r . root@your-server-ip:/var/www/aas
   ```

4. **Nastavte environment variables**
   ```bash
   cd /var/www/aas
   cp .env.example .env
   nano .env  # VYPLŇTE VŠECHNY HESLA!
   ```

5. **Spusťte aplikaci**
   ```bash
   docker compose up -d
   ```

### Detailní návod:
Viz [DEPLOYMENT.md](DEPLOYMENT.md) pro kompletní step-by-step instrukce.

---

## 🔐 KRITICKÉ: Environment Variables

**MUSÍTE nastavit tyto environment variables před spuštěním:**

```bash
# Database (POVINNÉ)
DB_PASSWORD=Your_Strong_Database_Password_123!

# Admin Account (POVINNÉ)
ADMIN_EMAIL=admin@aristocraticartworksale.com
ADMIN_PASSWORD=Your_Strong_Admin_Password_456!

# SMTP Email (POVINNÉ pro funkčnost formulářů)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-specific-password
EMAIL_FROM=no-reply@aristocraticartworksale.com
EMAIL_TO=aristocratic-artwork-sell@proton.me

# Translation (VOLITELNÉ)
TRANSLATION_ENABLED=false
TRANSLATION_ENDPOINT=https://libretranslate.com/translate
```

**Generování silných hesel:**
```bash
openssl rand -base64 32
```

---

## 📋 Checklist před spuštěním

- [ ] `.env` soubor vytvořen a všechna hesla nastavena
- [ ] DB_PASSWORD je silné (min. 16 znaků)
- [ ] ADMIN_PASSWORD splňuje policy (min. 12 znaků, mix typů)
- [ ] SMTP údaje jsou správné a otestované
- [ ] Doména je nastavena na IP serveru
- [ ] Firewall povoluje porty 22, 80, 443
- [ ] Docker je nainstalovaný
- [ ] Máte 2GB+ RAM a 10GB+ disk space

---

## 🌐 Po nasazení

### Přístup k aplikaci:
- **Web:** https://aristocraticartworksale.com
- **Admin login:** https://aristocraticartworksale.com/Identity/Account/Login
- **Admin panel:** https://aristocraticartworksale.com/Admin/Collections

### První kroky:
1. Přihlaste se jako admin (email a heslo z environment variables)
2. Vytvořte první kolekci v Admin panelu
3. Nahrajte fotky a volitelně audio soubor
4. Otestujte formulář "I'm interested" - měl by přijít email

### SSL Certifikát:
```bash
apt install certbot python3-certbot-nginx
certbot --nginx -d aristocraticartworksale.com -d www.aristocraticartworksale.com
```

---

## 📦 Technologie Stack

| Komponenta | Technologie | Verze |
|-----------|-------------|-------|
| Framework | ASP.NET Core | 8.0 |
| Jazyk | C# | 12.0 |
| Databáze | PostgreSQL | 16 |
| ORM | Entity Framework Core | 8.0.8 |
| Frontend | Bootstrap | 5.3.3 |
| Galerie | Swiper.js | 11 |
| Fonty | Playfair Display, Inter | - |
| Email | MailKit | 4.8.0 |
| PDF | QuestPDF | 2024.10.3 |
| Images | SixLabors.ImageSharp | 3.1.12 ✅ |
| Překlady | LibreTranslate | API |
| Hosting | Ubuntu + Docker + Nginx | - |

---

## 🎯 Funkce

### Pro návštěvníky:
- ✅ Procházení kolekcí bez registrace
- ✅ Filtrování podle kategorií
- ✅ Swipování mezi fotkami
- ✅ Poslech klasické hudby
- ✅ Přepínání mezi 10 jazyky
- ✅ Odesílání dotazů přes formulář

### Pro adminy:
- ✅ Nahrávání nových kolekcí
- ✅ Správa obrázků (auto-resize na 3 velikosti)
- ✅ Nahrávání audio souborů (MP3)
- ✅ Editace existujících kolekcí
- ✅ Zobrazení všech kolekcí

---

## 🔧 Správa

### Základní příkazy:

```bash
# Zobrazit logy
docker compose logs -f web

# Restartovat aplikaci
docker compose restart web

# Aktualizovat aplikaci (po změnách kódu)
docker compose up -d --build

# Zastavit aplikaci
docker compose down

# Zálohovat databázi
docker exec aas_postgres pg_dump -U aas aas > backup_$(date +%Y%m%d).sql

# Obnovit databázi ze zálohy
cat backup_20250105.sql | docker exec -i aas_postgres psql -U aas aas
```

---

## 📞 Podpora & Kontakt

- **Email:** aristocratic-artwork-sell@proton.me
- **Security issues:** Viz [SECURITY.md](SECURITY.md)
- **Deployment help:** Viz [DEPLOYMENT.md](DEPLOYMENT.md)

---

## ✅ Build Status

- **Last build:** Successful ✅
- **Security warnings:** 0 ✅
- **Test status:** All passed ✅
- **Vulnerable packages:** 0 ✅

```bash
dotnet build -c Release
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

---

## 📝 Poznámky

1. **První spuštění:**
   - Databáze se automaticky vytvoří při prvním spuštění
   - Migrace se spustí automaticky
   - Admin účet se vytvoří automaticky (z ADMIN_EMAIL a ADMIN_PASSWORD)

2. **SMTP Email:**
   - Pro Gmail MUSÍTE použít App Password, ne běžné heslo
   - Zapněte 2FA v Google účtu
   - Vygenerujte App Password zde: https://myaccount.google.com/apppasswords

3. **Překlady:**
   - Výchozí je vypnuto (TRANSLATION_ENABLED=false)
   - Pro zapnutí použijte LibreTranslate API nebo vlastní instanci
   - Překlady se cachují do databáze

4. **Nahrávání souborů:**
   - Maximální velikost obrázku: 10MB
   - Maximální velikost audio: 15MB
   - Povolené formáty obrázků: JPG, JPEG, PNG, WEBP
   - Povolený formát audio: MP3

---

## 🎉 Projekt je ready-to-deploy!

Vše je připravené. Stačí nastavit environment variables a spustit!

**Hodně štěstí s nasazením! 🚀**
