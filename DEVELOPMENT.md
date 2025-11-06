# Local Development Guide

## 🚀 Quick Start (3 kroky)

### 1. Prerequisites

Nainstaluj:
- **Docker Desktop** - https://www.docker.com/products/docker-desktop
- **.NET 8 SDK** - https://dotnet.microsoft.com/download
- **VS Code** - https://code.visualstudio.com

### 2. Setup Environment

```powershell
# Otevři PowerShell v root složce projektu
cd C:\AAS

# Spusť setup script
.\dev-setup.ps1
```

Script automaticky:
- ✅ Zkontroluje dependencies
- ✅ Spustí PostgreSQL v Dockeru
- ✅ Restoruje NuGet packages
- ✅ Aplikuje database migrations
- ✅ Vytvoří upload složky

### 3. Start Debugging

1. Otevři projekt ve VS Code: `code .`
2. **Stiskni F5** 🎉
3. Browser se automaticky otevře na `http://localhost:5000`

---

## 🎮 Debugging Options (F5)

VS Code nabízí 3 konfigurace:

### 1. 🚀 Launch Web (F5) - **DEFAULT**
- Normální debug mode
- Breakpointy fungují
- Browser se otevře automaticky

### 2. 🔧 Launch with Watch (Hot Reload)
- Automatický restart při změně kódu
- Hot reload pro rychlejší development
- Vybereš v Debug panelu

### 3. 🐛 Attach to Process
- Připojení k běžícímu procesu
- Pro advanced debugging

---

## 🗄️ Database Management

### PostgreSQL Docker Container

```bash
# Start PostgreSQL
docker-compose -f docker-compose.dev.yml up -d postgres

# Stop PostgreSQL
docker-compose -f docker-compose.dev.yml down

# View logs
docker logs aas_dev_postgres -f

# Connect to PostgreSQL CLI
docker exec -it aas_dev_postgres psql -U aas_dev -d aas_dev
```

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/AAS.Web

# Apply migrations
dotnet ef database update --project src/AAS.Web

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project src/AAS.Web

# Drop database and recreate
dotnet ef database drop --force --project src/AAS.Web
dotnet ef database update --project src/AAS.Web
```

### pgAdmin (Optional)

Web-based database UI:

```bash
# Start pgAdmin
docker-compose -f docker-compose.dev.yml up -d pgadmin

# Open in browser
http://localhost:5050

# Login:
Email: admin@localhost
Password: admin
```

**Add Server in pgAdmin:**
- Name: AAS Dev
- Host: postgres (Docker network)
- Port: 5432
- Username: aas_dev
- Password: dev_password_123

---

## 📧 Email Testing

### MailHog (Optional)

Fake SMTP server pro testování emailů:

```bash
# Start MailHog
docker-compose -f docker-compose.dev.yml up -d mailhog

# Open Web UI
http://localhost:8025
```

Všechny emaily odeslané z aplikace se objeví v MailHog UI.

**Update appsettings.Development.json:**
```json
{
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "UseStartTls": false
  }
}
```

---

## 🔐 Test Credentials

### Admin Account
```
Email: admin@localhost
Password: Admin123!@#
```

### Database
```
Host: localhost
Port: 5432
Database: aas_dev
Username: aas_dev
Password: dev_password_123
```

---

## 📁 Project Structure

```
C:\AAS\
├── .vscode/              # VS Code configuration
│   ├── launch.json       # Debug configurations (F5)
│   ├── tasks.json        # Build tasks
│   ├── settings.json     # Editor settings
│   └── extensions.json   # Recommended extensions
├── src/
│   └── AAS.Web/          # Main application
│       ├── Controllers/  # MVC & API controllers
│       ├── Models/       # Entity models
│       ├── Views/        # Razor views
│       ├── Services/     # Business logic
│       ├── wwwroot/      # Static files
│       │   └── uploads/  # User uploads (created by setup)
│       └── appsettings.Development.json  # Dev configuration
├── docker-compose.dev.yml   # Dev services (PostgreSQL, MailHog, pgAdmin)
├── dev-setup.ps1            # Setup script
└── DEVELOPMENT.md           # This file
```

---

## 🛠️ Common Tasks

### Build & Clean

```bash
# Build project
dotnet build src/AAS.Web

# Clean build artifacts
dotnet clean src/AAS.Web

# Restore packages
dotnet restore src/AAS.Web
```

### Run without Debugging

```bash
cd src/AAS.Web
dotnet run

# Or with watch (hot reload)
dotnet watch run
```

### Testing

```bash
# Run all tests (when you create them)
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

---

## 🔧 VS Code Tasks (Ctrl+Shift+P > Tasks: Run Task)

Dostupné tasky:
- **build** - Build projektu
- **clean** - Vyčištění build artifacts
- **restore** - Restore NuGet packages
- **watch** - Run s hot reload
- **Start PostgreSQL (Docker)** - Spustí DB
- **Stop PostgreSQL (Docker)** - Zastaví DB
- **Reset Database** - Dropne a vytvoří novou DB
- **Add Migration** - Vytvoří novou migraci

---

## 🐛 Troubleshooting

### PostgreSQL se nespustí

```bash
# Check if port 5432 is already in use
netstat -ano | findstr :5432

# Remove old container
docker rm -f aas_dev_postgres

# Recreate
docker-compose -f docker-compose.dev.yml up -d postgres
```

### Build fails with "target framework not found"

```bash
# Check installed .NET SDKs
dotnet --list-sdks

# Install .NET 8 if missing
winget install Microsoft.DotNet.SDK.8
```

### Database connection fails

```bash
# Check if PostgreSQL is running
docker ps | findstr postgres

# Check logs
docker logs aas_dev_postgres

# Test connection
docker exec -it aas_dev_postgres psql -U aas_dev -d aas_dev -c "SELECT 1"
```

### Hot reload not working

1. Ujisti se, že používáš "Launch with Watch" konfiguraci
2. Restartuj VS Code
3. Clean & rebuild: `dotnet clean && dotnet build`

---

## 🎯 Development Workflow

### Typical Day

1. **Start services:**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d postgres
   ```

2. **Open VS Code:**
   ```bash
   code .
   ```

3. **Press F5** to start debugging

4. **Make changes** - hot reload dělá restart automaticky

5. **Add migration when models change:**
   ```bash
   dotnet ef migrations add MyChanges --project src/AAS.Web
   ```

6. **End of day - stop services:**
   ```bash
   docker-compose -f docker-compose.dev.yml down
   ```

### Adding New Feature

1. Create branch: `git checkout -b feature/my-feature`
2. Make changes
3. Add migration if needed
4. Test locally (F5)
5. Commit: `git commit -m "Add my feature"`
6. Push: `git push origin feature/my-feature`

---

## 📦 VS Code Extensions

Doporučené extensions (auto-install prompt při otevření):

- **C# Dev Kit** - C# support
- **C#** - Intellisense & debugging
- **SQLTools** - Database management
- **Docker** - Docker support
- **GitLens** - Git integration
- **EditorConfig** - Code formatting

---

## 🚀 Performance Tips

### For faster builds:

```xml
<!-- Add to AAS.Web.csproj -->
<PropertyGroup>
  <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
  <RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
</PropertyGroup>
```

### For faster database:

```bash
# Use tmpfs for faster PostgreSQL (Linux/Mac)
docker-compose -f docker-compose.dev.yml up -d postgres --volume-driver local
```

---

## 📚 Additional Resources

- **ASP.NET Core Docs**: https://docs.microsoft.com/aspnet/core
- **Entity Framework Core**: https://docs.microsoft.com/ef/core
- **PostgreSQL Docs**: https://www.postgresql.org/docs
- **Docker Docs**: https://docs.docker.com

---

## 💡 Tips & Tricks

### Quick Admin Login

1. Navigate to `http://localhost:5000/Identity/Account/Login`
2. Use: `admin@localhost` / `Admin123!@#`

### View all emails in MailHog

1. Start MailHog: `docker-compose -f docker-compose.dev.yml up -d mailhog`
2. Open: `http://localhost:8025`
3. Send inquiry from web
4. See email in MailHog UI

### Reset everything

```bash
# Stop all services
docker-compose -f docker-compose.dev.yml down -v

# Remove all data
docker volume rm aas_postgres_dev_data

# Re-run setup
.\dev-setup.ps1
```

---

## 🎉 Happy Coding!

Máš-li otázky, podívej se do:
- `DEPLOYMENT.md` - Production deployment
- `SECURITY.md` - Security features
- `PERFORMANCE_FIXES.md` - Performance optimizations
- `PROJECT_SUMMARY.md` - Project overview
