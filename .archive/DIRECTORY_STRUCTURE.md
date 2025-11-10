# Kompletní adresářová struktura aplikace / Complete Directory Structure

## Přehled / Overview
Toto je ASP.NET Core aplikace "Aristocratic Artwork Sale" s PostgreSQL databází, Nginx reverse proxy a Docker Compose orchestrací.

```
/app (nebo /AAS na vašem serveru)
│
├── 📁 .emergent/                      # Emergent AI metadata
│   ├── emergent.yml
│   ├── summary.txt
│   └── markers/
│       └── .restore-complete
│
├── 📁 deployment/                     # Deployment skripty
│   ├── deploy.sh
│   ├── install.sh
│   └── update.sh
│
├── 📁 nginx/                          # Nginx konfigurace
│   ├── nginx.conf                     # Dev konfigurace
│   └── nginx.prod.conf               # ✅ Production konfigurace (opraveno pro CSS)
│
├── 📁 src/AAS.Web/                   # 🎯 HLAVNÍ APLIKACE
│   │
│   ├── 📁 Areas/                     # ASP.NET Core Areas
│   │   │
│   │   ├── 📁 Admin/                 # Admin area (správa kolekcí)
│   │   │   ├── Controllers/
│   │   │   │   └── CollectionsController.cs
│   │   │   └── Views/
│   │   │       ├── Collections/
│   │   │       │   ├── Create.cshtml
│   │   │       │   ├── Edit.cshtml
│   │   │       │   └── Index.cshtml
│   │   │       └── _ViewStart.cshtml
│   │   │
│   │   └── 📁 Identity/              # ASP.NET Identity (autentizace)
│   │       └── Pages/
│   │           ├── Account/
│   │           │   ├── Login.cshtml[.cs]
│   │           │   ├── Register.cshtml[.cs]
│   │           │   ├── ConfirmEmail.cshtml[.cs]
│   │           │   ├── RegisterConfirmation.cshtml[.cs]
│   │           │   └── Manage/       # Správa účtu
│   │           │       ├── Index.cshtml[.cs]
│   │           │       ├── Email.cshtml[.cs]
│   │           │       ├── ChangePassword.cshtml[.cs]
│   │           │       ├── PersonalData.cshtml[.cs]
│   │           │       ├── ManageNavPages.cs
│   │           │       ├── _Layout.cshtml
│   │           │       ├── _ManageNav.cshtml
│   │           │       └── _StatusMessage.cshtml
│   │           ├── _Layout.cshtml
│   │           ├── _ViewImports.cshtml
│   │           └── _ViewStart.cshtml
│   │
│   ├── 📁 Controllers/               # MVC Controllers
│   │   ├── HomeController.cs         # Homepage
│   │   ├── CollectionsController.cs  # Kolekce (veřejné)
│   │   ├── AboutController.cs        # O nás
│   │   ├── ContactController.cs      # Kontakt
│   │   ├── InquriesController.cs     # Dotazy
│   │   └── SitemapController.cs      # XML sitemap
│   │
│   ├── 📁 Database/                  # Entity Framework Core
│   │   ├── AppDbContext.cs           # DbContext (hlavní databázový kontext)
│   │   ├── AdminSeeder.cs            # Seed admin účtu
│   │   └── DesignTimeDbContextFactory.cs
│   │
│   ├── 📁 Migrations/                # EF Core migrace
│   │   ├── 20251106210415_InitialCreate.cs
│   │   ├── 20251108003259_AddCollectionTranslations.cs
│   │   ├── 20251108155050_SecurityAuditValidation.cs
│   │   └── AppDbContextModelSnapshot.cs
│   │
│   ├── 📁 Models/                    # Data modely
│   │   ├── Collection.cs             # Model kolekce
│   │   ├── CollectionImage.cs        # Obrázky kolekcí
│   │   ├── CollectionTranslation.cs  # Překlady kolekcí
│   │   ├── Inquiry.cs                # Dotazy od zákazníků
│   │   ├── TranslationCache.cs       # Cache překladů
│   │   └── Enum.cs                   # Enumerations
│   │
│   ├── 📁 Resources/                 # Lokalizace (i18n)
│   │   ├── SharedResources.cs
│   │   ├── SharedResources.resx      # Default (English)
│   │   ├── SharedResources.cs.resx   # Czech
│   │   ├── SharedResources.de.resx   # German
│   │   ├── SharedResources.es.resx   # Spanish
│   │   ├── SharedResources.fr.resx   # French
│   │   ├── SharedResources.hi.resx   # Hindi
│   │   ├── SharedResources.ja.resx   # Japanese
│   │   ├── SharedResources.pt.resx   # Portuguese
│   │   ├── SharedResources.ru.resx   # Russian
│   │   └── SharedResources.zh.resx   # Chinese
│   │
│   ├── 📁 Services/                  # Business logic services
│   │   ├── EmailService.cs           # Email odesílání (SMTP)
│   │   ├── EmailSenderAdapter.cs     # Adapter pro Identity
│   │   ├── ImageService.cs           # Správa obrázků
│   │   ├── SlugService.cs            # URL slugs
│   │   ├── TranslationService.cs     # Machine translation
│   │   ├── GoogleTranslateService.cs # Google Translate
│   │   └── TranslationHelper.cs      # Translation helpers
│   │
│   ├── 📁 Views/                     # Razor Views (MVC)
│   │   │
│   │   ├── 📁 Home/
│   │   │   └── Index.cshtml          # Homepage
│   │   │
│   │   ├── 📁 Collections/
│   │   │   ├── Index.cshtml          # Seznam kolekcí
│   │   │   └── Detail.cshtml         # Detail kolekce
│   │   │
│   │   ├── 📁 About/
│   │   │   └── Index.cshtml          # O nás stránka
│   │   │
│   │   ├── 📁 Contacts/
│   │   │   └── Index.cshtml          # Kontakt stránka
│   │   │
│   │   ├── 📁 Shared/                # Sdílené komponenty
│   │   │   ├── _Layout.cshtml        # Hlavní layout
│   │   │   ├── _LoginPartial.cshtml  # Login partial
│   │   │   └── Error.cshtml          # Error page
│   │   │
│   │   ├── _ViewImports.cshtml       # Global imports
│   │   └── _ViewStart.cshtml         # View startup
│   │
│   ├── 📁 wwwroot/                   # 🎨 STATIC FILES
│   │   ├── 📁 css/
│   │   │   ├── site.css              # Hlavní CSS (✅ funguje po opravě)
│   │   │   ├── site-new.css
│   │   │   └── site.css.backup
│   │   │
│   │   ├── 📁 js/
│   │   │   └── site.js               # Hlavní JavaScript
│   │   │
│   │   ├── 📁 images/
│   │   │   ├── logo.png              # Logo
│   │   │   └── logo-hero.png         # Hero logo
│   │   │
│   │   └── robots.txt                # SEO robots.txt
│   │
│   ├── Program.cs                    # 🚀 MAIN ENTRY POINT
│   ├── AAS.Web.csproj                # Project file
│   ├── appsettings.json              # Konfigurace (default)
│   ├── appsettings.Development.json  # Dev konfigurace
│   ├── appsettings.Production.json   # Prod konfigurace
│   └── appsettings.SECURITY.md       # Security dokumentace
│
├── 📄 CONFIGURATION FILES
│   ├── .env.production               # ✅ Environment variables (vytvořeno)
│   ├── .env.production.example       # Template pro .env
│   ├── docker-compose.yml            # Dev compose
│   ├── docker-compose.dev.yml        # Dev compose
│   ├── docker-compose.prod.yml       # ✅ Production compose (opraveno)
│   ├── docker-compose.host.yml       # Host database setup
│   ├── Dockerfile                    # Dev Dockerfile
│   ├── Dockerfile.prod               # Production Dockerfile
│   ├── docker-entrypoint.sh          # Container entrypoint
│   ├── .dockerignore
│   ├── .editorconfig
│   └── AAS.sln                       # Solution file
│
├── 📜 DEPLOYMENT SCRIPTS
│   ├── deploy.sh                     # Main deployment script
│   ├── restart-deployment.sh         # ✅ Restart s .env.production
│   ├── fix-css-and-restart.sh       # ✅ CSS fix + restart
│   ├── setup-production.sh           # Production setup
│   ├── setup-ssl.sh                  # SSL/certbot setup
│   ├── backup.sh                     # Backup script
│   └── dev-setup.ps1                 # Windows dev setup
│
├── 📚 DOCUMENTATION
│   ├── START_HERE.txt                # ⭐ Začněte zde
│   ├── PROJECT_SUMMARY.md            # Project overview
│   ├── FIX_DATABASE_CONNECTION.md    # ✅ DB fix dokumentace
│   ├── CSS_LOADING_FIX.md            # ✅ CSS fix dokumentace
│   ├── DIRECTORY_STRUCTURE.md        # 📍 TENTO SOUBOR
│   │
│   ├── QUICK_START.md                # Quick start guide
│   ├── QUICK_START_CZ.md             # Quick start (čeština)
│   ├── QUICK_START_PRODUCTION.md     # Production quick start
│   ├── DEVELOPMENT.md                # Dev guide
│   ├── NAVOD_SPUSTENI.md             # Návod ke spuštění (CZ)
│   │
│   ├── DEPLOYMENT.md                 # Deployment guide
│   ├── DEPLOY_MANUAL.md              # Manual deployment
│   ├── DEPLOY_README.md              # Deploy readme
│   ├── DEPLOYMENT_CHECKLIST.md       # Deployment checklist
│   ├── PRODUCTION_DEPLOYMENT.md      # Production deployment
│   │
│   ├── SECURITY.md                   # Security guide
│   ├── SECURITY_AUDIT_REPORT.md      # Security audit
│   ├── SECURITY_AUDIT_COMPLETE.md    # Audit completion
│   ├── SECURITY_MIGRATION_GUIDE.md   # Security migration
│   ├── SECURITY_QUICK_REFERENCE.md   # Security reference
│   ├── SECURITY-CHECKLIST.md         # Security checklist
│   │
│   ├── ADMIN_LOGIN_GUIDE.md          # Admin login guide
│   ├── HTTPS_CERTIFICATE_GUIDE.md    # HTTPS/SSL guide
│   ├── ARCHITECTURE_DIAGRAM.md       # Architecture
│   ├── BACKUP-AND-MONITORING.md      # Backup & monitoring
│   ├── SERVER_REQUIREMENTS.md        # Server requirements
│   ├── VS_CODE_SETUP_GUIDE.md        # VS Code setup
│   ├── NO_DOCKER_GUIDE.md            # Non-Docker setup
│   │
│   ├── HTTP_400_FIX_CARD.md          # HTTP 400 fix
│   ├── README_HTTP_400_FIX.md        # HTTP 400 readme
│   ├── STATIC_FILES_FIX.md           # Static files fix
│   ├── BUILD_ERROR_FIX.md            # Build error fix
│   ├── FIX_INDEX.md                  # Index of fixes
│   ├── CHANGES_SUMMARY.md            # Changes summary
│   │
│   └── README.md                     # Main readme
│
└── 📁 OTHER
    ├── setup-postgres-permissions.sql
    ├── debug-static-files.sh
    └── QUICK_FIX_COMMANDS.sh
```

## Docker Volumes (Runtime)

```
Docker Volumes vytvořené při běhu:
├── postgres-data/                    # PostgreSQL data
│   └── [PostgreSQL database files]
│
└── static-files/                     # Shared static files
    ├── css/
    │   └── site.css
    ├── js/
    │   └── site.js
    ├── images/
    │   ├── logo.png
    │   └── logo-hero.png
    ├── Identity/
    └── robots.txt
```

## Docker Containers (Runtime)

```
Running Containers:
├── aas-web-prod                      # ASP.NET Core app (port 5000)
├── aas-db-prod                       # PostgreSQL 15 (internal only)
├── aas-nginx-prod                    # Nginx (ports 80, 443)
└── aas-certbot                       # Certbot (SSL renewal)
```

## Klíčové soubory podle funkce / Key Files by Function

### 🚀 Application Entry Point
- **`/app/src/AAS.Web/Program.cs`** - Main application startup

### 🗄️ Database
- **`/app/src/AAS.Web/Database/AppDbContext.cs`** - EF Core DbContext
- **`/app/src/AAS.Web/Migrations/`** - Database migrations
- **`/app/.env.production`** - DB connection config

### 🎨 Frontend
- **`/app/src/AAS.Web/wwwroot/css/site.css`** - Main CSS
- **`/app/src/AAS.Web/wwwroot/js/site.js`** - Main JavaScript
- **`/app/src/AAS.Web/Views/`** - Razor views
- **`/app/src/AAS.Web/Areas/Identity/Pages/`** - Identity pages

### 🔧 Configuration
- **`/app/.env.production`** - ✅ Environment variables (vytvořeno)
- **`/app/docker-compose.prod.yml`** - ✅ Production compose (opraveno)
- **`/app/nginx/nginx.prod.conf`** - ✅ Nginx config (opraveno pro CSS)
- **`/app/src/AAS.Web/appsettings.json`** - App settings

### 🐳 Docker
- **`/app/Dockerfile.prod`** - Production Dockerfile
- **`/app/docker-entrypoint.sh`** - Container startup script
- **`/app/docker-compose.prod.yml`** - Production orchestration

### 🚀 Deployment
- **`/app/deploy.sh`** - Main deployment script
- **`/app/restart-deployment.sh`** - ✅ Quick restart script
- **`/app/fix-css-and-restart.sh`** - ✅ CSS fix script

### 📧 Services
- **`/app/src/AAS.Web/Services/EmailService.cs`** - Email
- **`/app/src/AAS.Web/Services/ImageService.cs`** - Images
- **`/app/src/AAS.Web/Services/TranslationService.cs`** - i18n

## Tech Stack / Technologický stack

```
┌─────────────────────────────────────────┐
│          Frontend Layer                 │
│  • Razor Views (MVC + Razor Pages)     │
│  • CSS (Bootstrap-like styling)        │
│  • JavaScript (Vanilla JS)             │
│  • ASP.NET Core Identity UI             │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│        Application Layer                │
│  • ASP.NET Core 8.0                     │
│  • MVC Controllers                      │
│  • Services (Email, Image, i18n)       │
│  • Entity Framework Core                │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│          Database Layer                 │
│  • PostgreSQL 15                        │
│  • EF Core Migrations                   │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│      Infrastructure Layer               │
│  • Docker Compose                       │
│  • Nginx (Reverse Proxy)                │
│  • Let's Encrypt (SSL)                  │
└─────────────────────────────────────────┘
```

## Aktuální stav / Current Status

✅ **Database Connection** - Opraveno (FIX_DATABASE_CONNECTION.md)
✅ **CSS Loading** - Opraveno (CSS_LOADING_FIX.md)
✅ **Static Files** - Funguje (Nginx konfigurace opravena)
✅ **Environment Variables** - Nakonfigurováno (.env.production)
✅ **Docker Health Checks** - Implementováno
✅ **ProtonMail Bridge** - Nakonfigurováno (host.docker.internal)

## Jak používat tuto strukturu / How to Use

### Pro vývoj / For Development
```bash
cd /app/src/AAS.Web
dotnet run
```

### Pro produkci / For Production
```bash
cd /app
./fix-css-and-restart.sh   # Restart s opravami
```

### Navigace / Navigation
```bash
# Aplikační kód
cd /app/src/AAS.Web/

# Konfigurace
cd /app/

# Dokumentace
ls /app/*.md

# Static files (source)
cd /app/src/AAS.Web/wwwroot/

# Nginx config
cd /app/nginx/
```

---

**Poznámka:** Toto je dynamický dokument. Aktualizujte ho při přidání nových souborů nebo změně struktury.
