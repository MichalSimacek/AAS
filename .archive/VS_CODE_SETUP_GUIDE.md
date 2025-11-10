# 🚀 VS Code Local Development - Complete Guide

## 📋 Co bylo vytvořeno

Projekt je nyní **plně nakonfigurován pro VS Code development** s jedním kliknutím na F5!

### Vytvořené soubory:

```
C:\AAS\
├── .vscode/
│   ├── launch.json        ✅ Debug konfigurace (F5)
│   ├── tasks.json         ✅ Build tasky
│   ├── settings.json      ✅ Editor nastavení
│   └── extensions.json    ✅ Doporučené extensions
├── src/AAS.Web/
│   └── appsettings.Development.json  ✅ Lokální dev config
├── docker-compose.dev.yml   ✅ PostgreSQL, MailHog, pgAdmin
├── dev-setup.ps1            ✅ Automatický setup script
├── .editorconfig            ✅ Code formatting rules
├── DEVELOPMENT.md           ✅ Detailní dev dokumentace
└── README_DEVELOPMENT.txt   ✅ Quick reference
```

---

## 🎯 Jak spustit projekt (JEDNODUCHÉ!)

### Metoda A: Automatický setup (DOPORUČENO)

```powershell
# 1. Otevři PowerShell jako Administrator
# 2. Naviguj do projektu
cd C:\AAS

# 3. Spusť setup script
.\dev-setup.ps1

# 4. Otevři VS Code
code .

# 5. Stiskni F5 🎉
```

**To je vše!** Browser se automaticky otevře na `http://localhost:5000`

---

### Metoda B: Manuální setup

Pokud chceš vědět co se děje pod kapotou:

```powershell
# 1. Start PostgreSQL
docker-compose -f docker-compose.dev.yml up -d postgres

# 2. Wait for PostgreSQL to be ready (30 sec)
Start-Sleep -Seconds 30

# 3. Restore packages
cd src\AAS.Web
dotnet restore

# 4. Apply migrations
dotnet ef database update

# 5. Create upload dirs
New-Item -ItemType Directory -Force wwwroot\uploads\images
New-Item -ItemType Directory -Force wwwroot\uploads\audio

# 6. Return to root and open VS Code
cd ..\..
code .

# 7. Press F5 in VS Code
```

---

## 🎮 Debug Konfigurace (F5)

VS Code nabízí **3 debug režimy**:

### 1. 🚀 Launch Web (DEFAULT)
```json
Stiskni F5 nebo vybereš "Launch Web (F5)" v Debug panelu
```

**Co dělá:**
- Automatický build projektu
- Spustí aplikaci na `http://localhost:5000` a `https://localhost:5001`
- Otevře browser automaticky
- Breakpointy fungují plně
- Console output v integrovaném terminálu

**Kdy použít:** Normální debugging, testování funkcionality

---

### 2. 🔧 Launch with Watch (Hot Reload)
```json
Vyber v Debug dropdown: "Launch with Watch (Hot Reload)"
```

**Co dělá:**
- Sleduje změny v kódu
- Automatický restart při změně
- Rychlejší development cycle
- Hot reload pro C# kód

**Kdy použít:** Aktivní vývoj s častými změnami

---

### 3. 🐛 Attach to Process
```json
Vyber v Debug dropdown: "Attach to Process"
```

**Co dělá:**
- Připojí se k běžícímu .NET procesu
- Debugging bez restartu aplikace
- Advanced debugging scenarios

**Kdy použít:** Debugging běžící aplikace, production debugging

---

## 🛠️ VS Code Tasks (Ctrl+Shift+P > Tasks: Run Task)

### Build & Clean:
- **build** - Build projektu (Ctrl+Shift+B)
- **clean** - Vyčištění bin/obj
- **restore** - Restore NuGet packages
- **publish** - Publish release build

### Database:
- **Reset Database** - Drop & recreate DB
- **Add Migration** - Vytvoří novou migraci (prompt pro název)

### Docker:
- **Start PostgreSQL (Docker)** - Spustí DB
- **Stop PostgreSQL (Docker)** - Zastaví DB

### Development:
- **watch** - Hot reload mode

---

## 🔐 Test Credentials

### Admin Account
```
URL: http://localhost:5000/Identity/Account/Login
Email: admin@localhost
Password: Admin123!@#
```

Po přihlášení jdi na: `http://localhost:5000/Admin/Collections`

### Database (PostgreSQL)
```
Host: localhost
Port: 5432
Database: aas_dev
Username: aas_dev
Password: dev_password_123
```

**Připojení z VS Code:**
1. Nainstaluj extension: "SQLTools PostgreSQL"
2. Přidej connection s výše uvedenými údaji
3. Máš SQL GUI přímo ve VS Code!

---

## 🌐 Optional Services

### MailHog - Email Testing

**Start:**
```bash
docker-compose -f docker-compose.dev.yml up -d mailhog
```

**Usage:**
1. Otevři: `http://localhost:8025`
2. Odešli inquiry z webu
3. Email se objeví v MailHog UI (žádný skutečný email)

**Stop:**
```bash
docker-compose -f docker-compose.dev.yml stop mailhog
```

---

### pgAdmin - Database UI

**Start:**
```bash
docker-compose -f docker-compose.dev.yml up -d pgadmin
```

**Login:**
- URL: `http://localhost:5050`
- Email: `admin@localhost`
- Password: `admin`

**Add Server:**
- Name: `AAS Dev`
- Host: `postgres` (Docker network name)
- Port: `5432`
- Username: `aas_dev`
- Password: `dev_password_123`

**Stop:**
```bash
docker-compose -f docker-compose.dev.yml stop pgadmin
```

---

## 📝 Typický Development Workflow

### Začátek dne:

```powershell
# 1. Start services
docker-compose -f docker-compose.dev.yml up -d postgres

# 2. Open VS Code
code .

# 3. Press F5
```

### Během dne:

```
1. Upravíš kód
2. F5 automaticky restartuje (nebo hot reload)
3. Testuj v browseru
4. Breakpointy v VS Code fungují
5. Opakuj
```

### Když měníš databázový model:

```bash
# 1. Uprav model (např. Collection.cs)

# 2. Vytvoř migraci
Ctrl+Shift+P > Tasks: Run Task > Add Migration
# Zadej název: "AddNewField"

# 3. Aplikuj migraci
dotnet ef database update --project src/AAS.Web

# 4. Restart aplikace (F5)
```

### Konec dne:

```bash
# Stop všechny services
docker-compose -f docker-compose.dev.yml down
```

---

## 🐛 Debugging Tips

### Breakpoints
```csharp
// Klikni na left margin vedle řádku
// Červená tečka = breakpoint
// F5 zastaví na breakpointu
// F10 = Step Over
// F11 = Step Into
// F5 = Continue
```

### Watch Variables
```
1. Když jsi na breakpointu
2. Najeď myší na proměnnou
3. Nebo přidej do Watch panelu
4. Vidíš hodnoty v real-time
```

### Debug Console
```
Můžeš psát C# výrazy přímo v Debug Console:
> item.Title
> _db.Collections.Count()
> DateTime.Now
```

---

## 🔧 Troubleshooting

### "Port 5432 is already in use"

```bash
# Zjisti co běží na portu
netstat -ano | findstr :5432

# Zastav starý container
docker ps
docker stop aas_dev_postgres
docker rm aas_dev_postgres

# Restart
docker-compose -f docker-compose.dev.yml up -d postgres
```

---

### "Cannot connect to database"

```bash
# Check if PostgreSQL is running
docker ps | findstr postgres

# Check logs
docker logs aas_dev_postgres

# Restart PostgreSQL
docker-compose -f docker-compose.dev.yml restart postgres
```

---

### "Build failed with target framework error"

```bash
# Check .NET SDK version
dotnet --version

# Should be 8.x.x
# If not, install .NET 8 SDK:
winget install Microsoft.DotNet.SDK.8
```

---

### "Extensions not loading"

```
1. Ctrl+Shift+X (Extensions)
2. Zkontroluj že máš nainstalované:
   - C# Dev Kit
   - C#
   - SQLTools (optional)
3. Reload VS Code
```

---

### "Hot reload not working"

```
1. Use "Launch with Watch" debug config
2. Nebo run manually: dotnet watch run
3. Save file (Ctrl+S)
4. Watch vidí změnu a restartuje
```

---

## 📚 Doporučené VS Code Extensions

Tyto se **automaticky navrhnou** při otevření projektu:

### Essentials:
- ✅ **C# Dev Kit** - C# development
- ✅ **C#** - IntelliSense, debugging

### Database:
- ✅ **SQLTools** - SQL queries ve VS Code
- ✅ **SQLTools PostgreSQL** - PostgreSQL driver

### Development:
- ✅ **Docker** - Docker support
- ✅ **GitLens** - Git supercharged
- ✅ **EditorConfig** - Code formatting

### Productivity:
- ✅ **vscode-icons** - Lepší ikony
- ✅ **TODO Highlight** - TODO/FIXME highlighting

**Install All:** Když VS Code navrhne, klikni "Install All"

---

## 🎯 Keyboard Shortcuts

### Essential:
- `F5` - Start debugging
- `Shift+F5` - Stop debugging
- `Ctrl+Shift+B` - Build
- `F9` - Toggle breakpoint
- `F10` - Step over
- `F11` - Step into

### Navigation:
- `Ctrl+P` - Quick file open
- `Ctrl+Shift+P` - Command palette
- `Ctrl+` ` - Toggle terminal
- `Ctrl+B` - Toggle sidebar

### Editing:
- `Ctrl+K Ctrl+C` - Comment line
- `Ctrl+K Ctrl+U` - Uncomment line
- `Alt+Up/Down` - Move line up/down
- `Shift+Alt+Down` - Duplicate line

---

## 📖 Další Dokumentace

- **DEVELOPMENT.md** - Detailní dev guide
- **DEPLOYMENT.md** - Production deployment
- **SECURITY.md** - Security features
- **PERFORMANCE_FIXES.md** - Performance optimizations
- **PROJECT_SUMMARY.md** - Project overview

---

## ✅ Setup Checklist

Po spuštění `dev-setup.ps1` by mělo být:

- [ ] Docker Desktop běží
- [ ] PostgreSQL container běží (`docker ps`)
- [ ] .NET 8 SDK nainstalovaný (`dotnet --version`)
- [ ] Packages restorované
- [ ] Database migrations aplikované
- [ ] Upload složky vytvořené
- [ ] VS Code otevřený v projektu
- [ ] F5 spustí aplikaci
- [ ] Browser otevře `http://localhost:5000`
- [ ] Admin login funguje
- [ ] Breakpointy ve VS Code fungují

---

## 🎉 You're Ready!

Máš kompletní development environment ready:

✅ **One-click debugging** (F5)
✅ **Hot reload** support
✅ **Database running** in Docker
✅ **Email testing** (MailHog)
✅ **Database UI** (pgAdmin)
✅ **All tools configured**

**Happy coding!** 🚀

---

**Questions?** Podívej se do `DEVELOPMENT.md` pro více detailů!
