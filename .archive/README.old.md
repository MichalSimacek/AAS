# Aristocratic Artwork Sale

Elegantní responzivní webová aplikace pro prodej uměleckých děl, šperků, hodinek a sběratelských předmětů.

## 🎨 Funkce

- **Instagram-like galerie** - swipování mezi fotkami s responzivními obrázky
- **Klasická hudba** - možnost přidat audio k jednotlivým kolekcím
- **10 jazyků** - automatický překlad (EN, CS, RU, DE, ES, FR, ZH, PT, HI, JA)
- **Kontaktní formuláře** - s odesíláním PDF na email
- **Admin panel** - pro správu kolekcí a nahrávání obsahu
- **SEO optimalizace** - meta tagy, schema.org, sitemap
- **Bezpečnost** - CSP, rate limiting, HTTPS

## 🛠️ Technologie

- **Backend:** ASP.NET Core 9.0 (C#)
- **Database:** PostgreSQL 16
- **Frontend:** Bootstrap 5, Swiper.js
- **Fonts:** Playfair Display, Inter
- **Email:** MailKit + QuestPDF
- **Images:** SixLabors.ImageSharp
- **Translation:** LibreTranslate

## 📂 Struktura projektu

```
AAS/
├── src/
│   └── AAS.Web/              # Hlavní webová aplikace
│       ├── Controllers/      # MVC controllery
│       ├── Models/           # Databázové modely
│       ├── Views/            # Razor views
│       ├── Services/         # Business logika
│       ├── Database/         # EF Core context
│       ├── wwwroot/          # Statické soubory
│       └── Resources/        # Lokalizační soubory
├── deployment/               # Deployment skripty
├── docker-compose.yml        # Docker konfigurace
├── Dockerfile                # Docker image
└── DEPLOYMENT.md            # Návod na nasazení
```

## 🚀 Rychlý start (Docker)

```bash
# Naklonujte repozitář
git clone https://github.com/your-repo/aas.git
cd aas

# Upravte SMTP nastavení v src/AAS.Web/appsettings.json

# Spusťte aplikaci
docker compose up -d

# Otevřete v prohlížeči
http://localhost:5000
```

## 📖 Dokumentace

Detailní návod na nasazení najdete v [DEPLOYMENT.md](DEPLOYMENT.md)

## 🔐 Admin přístup

Admin účet je vytvořen při prvním spuštění pomocí environment variables:

- **Email:** Nastavte přes `ADMIN_EMAIL` environment variable
- **Heslo:** Nastavte přes `ADMIN_PASSWORD` environment variable (minimálně 12 znaků!)
- **Admin panel:** /Admin/Collections
- **Login:** /Identity/Account/Login

⚠️ **KRITICKÉ:** NIKDY nepoužívejte slabá hesla! Viz [SECURITY.md](SECURITY.md)

## 🌐 Kategorie kolekcí

1. **Paintings** - Obrazy a malby
2. **Jewelry** - Šperky
3. **Watches** - Hodinky
4. **Statues** - Sochy
5. **Other** - Ostatní sběratelské předměty

## 📧 Email konfigurace

Pro odesílání emailů je potřeba nakonfigurovat SMTP server v `appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseStartTls": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "To": "aristocratic-artwork-sell@proton.me"
  }
}
```

## 🌍 Podporované jazyky

- 🇬🇧 English (EN)
- 🇨🇿 Čeština (CS)
- 🇷🇺 Русский (RU)
- 🇩🇪 Deutsch (DE)
- 🇪🇸 Español (ES)
- 🇫🇷 Français (FR)
- 🇨🇳 中文 (ZH)
- 🇵🇹 Português (PT)
- 🇮🇳 हिन्दी (HI)
- 🇯🇵 日本語 (JA)

## 🔒 Bezpečnost

### Implementované bezpečnostní funkce:

- ✅ **HTTPS/TLS** - Vynucené šifrované připojení
- ✅ **Content Security Policy (CSP)** - Ochrana proti XSS
- ✅ **Rate limiting** - 3 dotazy/15 min na IP adresu
- ✅ **Anti-forgery tokens** - Ochrana proti CSRF
- ✅ **SQL injection prevence** - Parametrizované dotazy (EF Core)
- ✅ **XSS prevence** - Automatické escapování HTML
- ✅ **Validace souborů** - Whitelist typů, size limity, verifikace obsahu
- ✅ **Security headers** - X-Frame-Options, X-Content-Type-Options, atd.
- ✅ **No hardcoded secrets** - Vše přes environment variables
- ✅ **Strong password policy** - Min. 12 znaků, mix typů
- ✅ **Request size limits** - 100MB max
- ✅ **Error handling** - Žádné stack traces v produkci

📖 **Detailní bezpečnostní dokumentace:** [SECURITY.md](SECURITY.md)

## 📦 Databázové migrace

```bash
# Vytvořit novou migraci
dotnet ef migrations add MigrationName

# Aplikovat migrace
dotnet ef database update
```

Migrace se automaticky aplikují při startu aplikace.

## 🎯 Funkcionality

### Pro návštěvníky
- Procházení kolekcí bez registrace
- Filtrování podle kategorií
- Swipování mezi fotkami v galeriích
- Poslech klasické hudby
- Přepínání mezi jazyky
- Odesílání dotazů přes formulář

### Pro adminy
- Nahrávání nových kolekcí
- Správa obrázků (automatické vytváření 3 velikostí)
- Nahrávání audio souborů
- Editace existujících kolekcí
- Zobrazení všech kolekcí

## 📝 License

Proprietární software - všechna práva vyhrazena.

## 📞 Kontakt

**Email:** aristocratic-artwork-sell@proton.me
**Web:** https://aristocraticartworksale.com

---

© 2025 Aristocratic Artwork Sale
