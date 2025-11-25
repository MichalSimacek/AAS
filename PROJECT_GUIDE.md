# 🏛️ Aristocratic Artwork Sale - Complete Project Guide

> **🔴 CRITICAL:** This document MUST be read before making any changes to the project!

---

## 📋 Table of Contents
1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Key Features](#key-features)
5. [Development Setup](#development-setup)
6. [Deployment](#deployment)
7. [Backup System](#backup-system)
8. [Known Issues & Solutions](#known-issues--solutions)
9. [Best Practices](#best-practices)

---

## 🎯 Project Overview

**Name:** Aristocratic Artwork Sale (AAS)  
**Description:** Online platform for selling aristocratic artwork, antiques, and collectibles  
**Production URL:** https://aristocraticartworksale.com  
**Production Server:** `/AAS` directory  
**Persistent Storage:** `/mnt/data`

---

## 🛠️ Technology Stack

### Backend
- **Framework:** ASP.NET Core 8.0 (MVC + Razor Pages)
- **Language:** C# 12
- **ORM:** Entity Framework Core 8.0
- **Database:** PostgreSQL 15

### Frontend
- **Views:** Razor Pages (.cshtml)
- **CSS Framework:** Bootstrap 5.3.3
- **Icons:** Bootstrap Icons
- **Fonts:** Google Fonts (Inter, Playfair Display)
- **Image Slider:** Swiper.js

### Infrastructure
- **Containerization:** Docker + Docker Compose
- **Web Server:** Nginx (Reverse Proxy + Static Files)
- **SSL/TLS:** Certbot (Let's Encrypt)
- **Email:** SMTP (Configured via environment variables)
- **Backup:** FTP to Master.cz (backup15.master.cz)

### Additional Services
- **Anti-DDoS:** Riorey Protection
- **Monitoring:** NRPE (Nagios)
- **Hosting:** Master.cz VPS

---

## 📁 Project Structure

```
/AAS/                                    # ⭐ ROOT DIRECTORY (PRODUCTION)
├── src/
│   └── AAS.Web/
│       ├── AAS.Web.csproj
│       ├── Program.cs                   # Application startup
│       ├── appsettings.json
│       ├── appsettings.Production.json
│       │
│       ├── Controllers/
│       │   ├── CollectionsController.cs
│       │   ├── InquriesController.cs    # Note: typo in filename
│       │   ├── CommentsController.cs
│       │   └── HowToController.cs
│       │
│       ├── Models/
│       │   ├── Collection.cs
│       │   ├── Inquiry.cs
│       │   ├── Comment.cs
│       │   ├── Enum.cs
│       │   └── ApplicationUser.cs
│       │
│       ├── Data/
│       │   └── AppDbContext.cs
│       │
│       ├── Services/
│       │   ├── EmailService.cs
│       │   └── EmailSenderAdapter.cs
│       │
│       ├── Areas/
│       │   ├── Admin/
│       │   │   ├── Controllers/
│       │   │   │   ├── DashboardController.cs
│       │   │   │   ├── CollectionsController.cs
│       │   │   │   └── InquiriesController.cs
│       │   │   └── Views/
│       │   │       ├── Dashboard/Index.cshtml
│       │   │       ├── Collections/Index.cshtml
│       │   │       └── Inquiries/Index.cshtml
│       │   └── Identity/
│       │       └── Pages/Account/
│       │
│       ├── Resources/                   # Localization (10 languages)
│       │   ├── SharedResources.cs.resx  # Czech (default)
│       │   ├── SharedResources.resx     # English
│       │   ├── SharedResources.de.resx  # German
│       │   ├── SharedResources.es.resx  # Spanish
│       │   ├── SharedResources.fr.resx  # French
│       │   ├── SharedResources.hi.resx  # Hindi
│       │   ├── SharedResources.ja.resx  # Japanese
│       │   ├── SharedResources.pt.resx  # Portuguese
│       │   ├── SharedResources.ru.resx  # Russian
│       │   └── SharedResources.zh.resx  # Chinese
│       │
│       ├── Views/
│       │   ├── Home/Index.cshtml
│       │   ├── Collections/
│       │   │   ├── Index.cshtml         # With pagination
│       │   │   └── Detail.cshtml        # With social share
│       │   ├── HowTo/Index.cshtml
│       │   ├── Contacts/Index.cshtml
│       │   └── Shared/
│       │       └── _Layout.cshtml
│       │
│       └── wwwroot/
│           ├── css/site.css             # Main stylesheet
│           ├── js/site.js               # Main JavaScript
│           └── uploads/                 # Uploaded files
│
├── nginx/
│   └── nginx.prod.conf                  # Nginx configuration
│
├── docker-compose.prod.yml              # Production Docker setup
├── Dockerfile.prod                      # Multi-stage build
├── backup-setup.sh                      # Backup installation script
├── setup-remote-sync.sh                 # Remote backup setup
└── PROJECT_GUIDE.md                     # This file

/mnt/data/                               # ⭐ PERSISTENT STORAGE
├── postgres/                            # PostgreSQL data
├── uploads/                             # Uploaded images
├── logs/                                # Application logs
└── backups/                             # Database backups
```

---

## ✨ Key Features

### Public Features
- ✅ **Multi-language support** (10 languages with automatic detection)
- ✅ **Collection browsing** with pagination (12 per page)
- ✅ **Collection detail** with image gallery (lightbox)
- ✅ **Social sharing** (Facebook, Twitter/X, LinkedIn, Copy Link)
- ✅ **Inquiry forms** (with email notifications)
- ✅ **Comment system** (authenticated users)
- ✅ **Responsive design** (mobile-friendly)
- ✅ **Status-based sorting** (Available → In Auction → Sold)

### Admin Features
- ✅ **Modern dashboard** with statistics
- ✅ **Collection management** (Create, Edit, Delete)
- ✅ **Image upload** with sortable gallery
- ✅ **Inquiry inbox** (view customer messages)
- ✅ **User management** (ASP.NET Identity)

### Technical Features
- ✅ **No Bootstrap modal issues** (custom implementation)
- ✅ **Optimized typography** (7 font sizes, 6 colors)
- ✅ **FOUC prevention** (critical CSS inline)
- ✅ **Floating labels** (Login/Register forms)
- ✅ **Automatic backups** (daily at 2:00 AM)
- ✅ **Docker containerization** (with health checks)

---

## 🚀 Development Setup

### Prerequisites
- .NET SDK 8.0
- Docker & Docker Compose
- PostgreSQL client (optional, for local development)

### Local Development (Not Recommended - Use Production Server)

This project is designed to run directly on the production server at `/AAS`. Local development is not fully supported.

---

## 📦 Deployment

### Production Deployment Process

```bash
# 1. Connect to server
ssh root@YOUR_SERVER_IP

# 2. Navigate to project
cd /AAS

# 3. Pull latest changes
git pull

# 4. Rebuild containers (with cache clearing if needed)
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml build --no-cache  # Use --no-cache if migrations changed
docker compose -f docker-compose.prod.yml up -d

# 5. Check logs
docker logs aas-web-prod -f --tail=100
```

### Docker Services

```yaml
Services:
  - web:        ASP.NET Core application (port 5000)
  - db:         PostgreSQL 15 (internal only)
  - nginx:      Reverse proxy (ports 80, 443)
```

### Environment Variables

Critical variables in `/AAS/src/AAS.Web/.env`:

```env
# Database
DB_HOST=db
DB_PORT=5432
DB_NAME=aas_production
DB_USER=aasuser
DB_PASSWORD=your_password

# Admin Account
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=your_password

# Email SMTP
EMAIL_SMTP_HOST=smtp.example.com
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=your_email
EMAIL_PASSWORD=your_password
EMAIL_FROM=noreply@aristocraticartworksale.com
EMAIL_TO=info@aristocraticartworksale.com
```

---

## 💾 Backup System

### Automated Backups

**Schedule:** Daily at 2:00 AM  
**Retention:** 7 days local, unlimited on remote FTP  
**Location:** `/AAS/local-backups/` (local) + Master.cz FTP (remote)

### What Gets Backed Up

1. **PostgreSQL Database** (compressed with gzip)
2. **Uploaded Files** (`/mnt/data/uploads`)
3. **Configuration Files** (`docker-compose.prod.yml`, `.env`)
4. **Backup Metadata** (backup_info.txt)

### Backup Configuration

**FTP Server:** backup15.master.cz  
**Username:** bcp-id-9316  
**Password:** Stored in `/root/.backup_credentials`  
**Capacity:** 100 GB

### Manual Backup

```bash
# Run backup manually
/AAS/backup.sh

# Check backup logs
tail -f /var/log/aas-backup.log

# List backups
ls -lh /AAS/local-backups/
```

### Restore from Backup

```bash
# 1. Choose backup
cd /AAS/local-backups/20251125_020000/

# 2. Restore database
gunzip database.sql.gz
docker exec -i aas-db-prod psql -U aasuser aas_production < database.sql

# 3. Restore uploads
tar -xzf uploads.tar.gz -C /AAS/

# 4. Set permissions
chown -R 33:33 /AAS/uploads  # www-data user

# 5. Restart services
cd /AAS
docker compose -f docker-compose.prod.yml restart
```

### Backup Setup (First Time)

```bash
# 1. Run backup setup
cd /AAS
chmod +x backup-setup.sh setup-remote-sync.sh
./backup-setup.sh

# 2. Get password from:
# https://admin.masterdc.com/sharing/showpass?id=2908&hash=9081592-4602710001763-3161599

# 3. Configure remote sync
./setup-remote-sync.sh

# 4. Test backup
./backup.sh
```

---

## 🐛 Known Issues & Solutions

### Issue #1: Duplicate Inquiry Emails

**Solution:** Implemented debouncing with `isSubmitting` flag in `site.js`

### Issue #2: Bootstrap Modal Problems

**Solution:** Replaced all Bootstrap modals with custom implementation using `display: none/flex`

### Issue #3: FOUC (Flash of Unstyled Content)

**Solution:** Added critical CSS inline in `<head>`, changed font-display to `optional`

### Issue #4: Floating Labels Not Working

**Solution:** Added `.identity-card` wrapper, fixed CSS conflicts with `transform: none !important`

### Issue #5: Typography Inconsistency

**Solution:** Reduced from 11 font sizes to 7, and 11 colors to 6

---

## ✅ Best Practices

### Before Making Changes

- [ ] Read this guide completely
- [ ] Check current git branch
- [ ] Test changes locally if possible
- [ ] Create backup before major changes

### Code Style

- [ ] Use semantic HTML5 elements
- [ ] Follow C# naming conventions (PascalCase for classes/methods)
- [ ] Keep controllers thin, logic in services
- [ ] Use async/await for all I/O operations
- [ ] Add try-catch for error handling

### CSS Guidelines

- [ ] Use existing typography scale (14, 16, 18, 20, 24, 32, 40px)
- [ ] Use color palette (#1A1A1A, #4A4A4A, #B8941F, #FFFFFF, #DC3545, #6c757d)
- [ ] Avoid !important unless absolutely necessary
- [ ] Prefix custom CSS classes with project-specific names
- [ ] Use `@@media` instead of `@media` in .cshtml files (Razor escape)

### JavaScript Guidelines

- [ ] Use `const` and `let`, avoid `var`
- [ ] Add event listener cleanup
- [ ] Debounce expensive operations
- [ ] Use `async/await` for fetch calls
- [ ] Add loading states for buttons

### Database Guidelines

- [ ] Always use async methods (`ToListAsync`, `FirstOrDefaultAsync`)
- [ ] Use `.AsNoTracking()` for read-only queries
- [ ] Include related entities explicitly
- [ ] Add indexes for frequently queried columns
- [ ] Use transactions for multi-step operations

### Deployment Guidelines

- [ ] Test on staging/development first
- [ ] Use `--no-cache` when changing migrations
- [ ] Check Docker logs after deployment
- [ ] Verify database migrations applied
- [ ] Test critical user flows (login, inquiry, etc.)

---

## 🔧 Common Commands

### Docker

```bash
# View logs
docker logs aas-web-prod -f --tail=100
docker logs aas-db-prod -f --tail=100

# Restart services
docker compose -f docker-compose.prod.yml restart

# Rebuild
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml build --no-cache
docker compose -f docker-compose.prod.yml up -d

# Access container
docker exec -it aas-web-prod bash
docker exec -it aas-db-prod psql -U aasuser -d aas_production
```

### Database

```bash
# Connect to PostgreSQL
docker exec -it aas-db-prod psql -U aasuser -d aas_production

# Useful SQL
\dt                    # List tables
\d "Collections"       # Table structure
SELECT * FROM "__EFMigrationsHistory";  # Migration history

# Backup
docker exec aas-db-prod pg_dump -U aasuser aas_production > backup.sql

# Restore
docker exec -i aas-db-prod psql -U aasuser -d aas_production < backup.sql
```

### Monitoring

```bash
# Check disk usage
df -h /mnt/data

# Check backup status
tail -f /var/log/aas-backup.log

# Check cron jobs
crontab -l

# Service status
systemctl status cron
systemctl status nagios-nrpe-server
```

---

## 📞 Support & Contact

**Hosting:** Master.cz  
**Anti-DDoS:** Riorey Protection  
**Backup:** FTP (backup15.master.cz)  
**Monitoring:** NRPE/Nagios

### Key URLs
- **Production:** https://aristocraticartworksale.com
- **Admin:** https://aristocraticartworksale.com/Admin
- **Backup Password:** https://admin.masterdc.com/sharing/showpass?id=2908&hash=9081592-4602710001763-3161599

---

## 📝 Changelog

### 2025-01-25
- ✅ Added automated backup system (daily 2AM)
- ✅ Configured remote FTP sync to Master.cz
- ✅ Set up NRPE monitoring
- ✅ Updated PROJECT_GUIDE.md with complete information

### 2025-01-20
- ✅ Modernized Admin Dashboard
- ✅ Improved Collections Management UI
- ✅ Fixed all Bootstrap modal issues

### 2025-01-19
- ✅ Added social sharing buttons
- ✅ Implemented pagination for collections
- ✅ Fixed inquiry form duplicate emails
- ✅ Simplified typography system
- ✅ Fixed FOUC and floating labels

---

**Last Updated:** 2025-01-25  
**Version:** 1.2  
**Status:** ✅ Production Ready

---

## 🚨 CRITICAL REMINDERS

```
╔══════════════════════════════════════════════════════════╗
║                                                          ║
║  🔴 PRODUCTION PATHS - NEVER FORGET!                    ║
║                                                          ║
║  ✅ Root Directory:      /AAS                           ║
║  ✅ Persistent Storage:  /mnt/data                      ║
║  ✅ Backups:             /AAS/local-backups             ║
║                                                          ║
║  ❌ NEVER use:           /app                           ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
```

**Before starting work:**
- [ ] Read this guide
- [ ] Check backup status
- [ ] Pull latest changes
- [ ] Test in non-production environment if possible
