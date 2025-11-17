# 🏛️ Aristocratic Artwork Sale - Developer Guide

> **🔴 KRITICKÉ:** Tento dokument MUSÍ být přečten před jakýmikoliv změnami v projektu!
> Obsahuje kritické poznatky získané během rozsáhlého debuggingu a deployment procesu.

---

## ⚠️ DŮLEŽITÉ - CESTY V PROJEKTU

### 🔴 KOŘENOVÝ ADRESÁŘ
**Produkční server:** `/AAS` (NIKDY ne `/app`!)  
**Persistent storage:** `/mnt/data` (databáze, nahrané soubory, logy)

**❌ NEPOUŽÍVEJ:** `/app` - to je jen development prostředí!  
**✅ POUŽÍVEJ:** `/AAS` - to je produkční cesta!

---

## 📋 Obsah
1. [Přehled projektu](#přehled-projektu)
2. [Struktura projektu a cesty](#struktura-projektu-a-cesty)
3. [Entity Framework Core - KRITICKÉ POZNATKY](#entity-framework-core---kritické-poznatky)
4. [Deployment proces](#deployment-proces)
5. [Databázová struktura](#databázová-struktura)
6. [Persistent Storage (/mnt/data)](#persistent-storage-mntdata)
7. [Důležité příkazy](#důležité-příkazy)
8. [Known Issues & Solutions](#known-issues--solutions)
9. [Best Practices](#best-practices)

---

## 🎯 Přehled projektu

**Název:** Aristocratic Artwork Sale (AAS)  
**Framework:** ASP.NET Core 8.0 (MVC + Razor Views)  
**Databáze:** PostgreSQL  
**ORM:** Entity Framework Core  
**Kontejnerizace:** Docker + Docker Compose  
**Web Server:** Nginx (Reverse Proxy)  
**Kořenový adresář:** `/AAS` (production)  
**Persistent storage:** `/mnt/data`

**Aktuální funkce:**
- ✅ Správa uměleckých sbírek (Collections)
- ✅ Autentizace a autorizace uživatelů
- ✅ Blog systém (BlogPosts)
- ✅ Komentáře u sbírek (Comments)
- ✅ "AAS Verified" odznak pro ověřené sbírky
- ⚠️ DeepL překladová služba (registrována, ale neimplementována)

---

## 📁 Struktura projektu a cesty

### 🔴 PRODUKČNÍ PROSTŘEDÍ

```
/AAS/                                # ⭐ KOŘENOVÝ ADRESÁŘ (PRODUCTION)
├── AAS.sln                          # Solution file
├── docker-compose.prod.yml          # Production Docker Compose
├── Dockerfile.prod                  # Production Dockerfile (multi-stage build)
├── nginx.conf                       # Nginx konfigurace (reverse proxy)
├── PROJECT_GUIDE.md                 # ⭐ TENTO SOUBOR - přečti před změnami!
├── test_result.md                   # Testing protokol a výsledky
│
└── src/
    └── AAS.Web/
        ├── AAS.Web.csproj           # Project file
        ├── Program.cs               # ⚠️ Startup logika + auto-migrace
        ├── appsettings.json         # Konfigurace (development)
        ├── appsettings.Production.json  # Konfigurace (production)
        │
        ├── Controllers/             # MVC Controllers
        │   ├── AccountController.cs
        │   ├── CollectionsController.cs
        │   ├── BlogController.cs    # Blog management
        │   └── CommentsController.cs # Comment system
        │
        ├── Models/                  # Data models
        │   ├── Collection.cs        # ⚠️ Obsahuje AASVerified property
        │   ├── BlogPost.cs          # Blog model
        │   ├── Comment.cs           # Comment model
        │   └── ApplicationUser.cs
        │
        ├── Data/
        │   └── AppDbContext.cs      # ⚠️ EF Core DbContext - KRITICKÝ
        │
        ├── Migrations/              # ⚠️⚠️⚠️ KRITICKÁ SLOŽKA!
        │   │                        # Viz sekce "EF Core - KRITICKÉ POZNATKY"
        │   ├── 20251106210415_InitialCreate.cs
        │   ├── 20251108003259_AddCollectionTranslations.cs
        │   ├── 20251108155050_SecurityAuditValidation.cs
        │   ├── 20251117232553_AddPriceStatusVerified.cs
        │   ├── 20251117232553_AddPriceStatusVerified.Designer.cs
        │   ├── 20251117232619_AddCommentsAndBlog.cs  # ⚠️ Byla PRÁZDNÁ
        │   ├── 20251117232619_AddCommentsAndBlog.Designer.cs
        │   └── AppDbContextModelSnapshot.cs
        │
        ├── Services/
        │   ├── DeepLService.cs      # ⚠️ Registrována, ale nepoužívá se
        │   └── EmailService.cs
        │
        ├── Resources/               # Lokalizační RESX soubory
        │   ├── Views.Home.Index.en.resx
        │   ├── Views.Home.Index.cs.resx
        │   └── ...                  # ⚠️ Některé překlady chybí
        │
        └── Views/                   # Razor views
            ├── Blog/
            ├── Collections/
            ├── Comments/
            └── Shared/

/mnt/data/                           # ⭐ PERSISTENT STORAGE
├── postgres/                        # PostgreSQL data (databázové soubory)
├── uploads/                         # Nahrané soubory (obrázky sbírek, atd.)
├── logs/                            # Aplikační logy
└── backups/                         # Databázové zálohy
```

### 🔴 PRAVIDLA PRO CESTY

1. **Vždy používej `/AAS` jako kořenový adresář v produkci**
2. **Persistent data MUSÍ být v `/mnt/data`** (jinak se ztratí při restartu kontejneru!)
3. **NIKDY nepiš hardcoded `/app`** - to je jen development
4. **Volume mappings v docker-compose.yml musí ukazovat na `/mnt/data`**

**Příklad správné konfigurace v docker-compose:**
```yaml
volumes:
  - /mnt/data/postgres:/var/lib/postgresql/data
  - /mnt/data/uploads:/AAS/wwwroot/uploads
  - /mnt/data/logs:/AAS/logs
```

---

## ⚠️ Entity Framework Core - KRITICKÉ POZNATKY

### 🔴 HLAVNÍ PROBLÉMY, KTERÉ BYLY ŘEŠENY

#### 1. **Duplicitní složky migrací**
**Problém:** Existovaly DVĚ složky:
- `/src/AAS.Web/Database/Migrations/` (stará, nesprávná)
- `/src/AAS.Web/Migrations/` (správná)

**Důsledek:** EF Core nemohlo najít nové migrace.

**Řešení:** Všechny migrace byly konsolidovány do `/src/AAS.Web/Migrations/`.

**⚠️ PRAVIDLO:** Vždy kontroluj, že existuje pouze JEDNA složka `Migrations`!

**Správná cesta:** `/AAS/src/AAS.Web/Migrations/`

---

#### 2. **Chybějící .Designer.cs soubory**
**Problém:** Migrace bez `.Designer.cs` souborů jsou pro EF Core **neplatné**!

**Příklad:**
```
✅ SPRÁVNĚ:
20251117232553_AddPriceStatusVerified.cs
20251117232553_AddPriceStatusVerified.Designer.cs

❌ ŠPATNĚ:
20251117232553_AddPriceStatusVerified.cs
(chybí Designer.cs)
```

**⚠️ PRAVIDLO:** Každá migrace MUSÍ mít svůj `.Designer.cs` soubor!

---

#### 3. **Nesprávný formát názvů migrací**
**Problém:** Některé migrace měly nesprávný formát názvu nebo chybný rok.

**Správný formát:**
```
YYYYMMDDHHMMSS_MigraceName.cs
```

**Příklady:**
```
✅ 20251117232553_AddPriceStatusVerified.cs
❌ 20211117232553_... (špatný rok)
❌ AddPriceStatusVerified.cs (chybí timestamp)
```

---

#### 4. **Prázdná migrace AddCommentsAndBlog**
**Problém:** Migrace `20251117232619_AddCommentsAndBlog.cs` byla vygenerována s prázdnými metodami:

```csharp
public partial class AddCommentsAndBlog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ⚠️ PRÁZDNÉ!
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ⚠️ PRÁZDNÉ!
    }
}
```

**Důsledek:** Tabulky `BlogPosts` a `Comments` nebyly vytvořeny.

**Dočasné řešení:** Tabulky vytvořeny manuálně pomocí SQL:
```sql
CREATE TABLE "BlogPosts" (...);
CREATE TABLE "Comments" (...);
```

**⚠️ ROOT CAUSE:** Neznámý - možný problém s EF Core konfigurací nebo generátorem.

**TODO:** Vyšetřit, proč byla migrace vygenerována prázdná.

---

### 🛠️ Jak správně pracovat s migracemi

#### Přidání nové migrace:

```bash
# 1. Vstup do SDK kontejneru (pokud není SDK na productionu)
docker run -it --rm \
  -v /app:/app \
  -w /app/src/AAS.Web \
  --network aas_default \
  -e ConnectionStrings__DefaultConnection="Host=db;Database=aasdb;Username=aasuser;Password=aaspassword" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash

# 2. Instalace EF Core tools
dotnet tool install --global dotnet-ef --version 8.0.11

# 3. Přidání migrace
dotnet ef migrations add MigrationName

# 4. Kontrola, že byly vytvořeny OBA soubory:
ls Migrations/
# Mělo by zobrazit:
# YYYYMMDDHHMMSS_MigrationName.cs
# YYYYMMDDHHMMSS_MigrationName.Designer.cs

# 5. Kontrola obsahu migrace (nesmí být prázdná!)
cat Migrations/YYYYMMDDHHMMSS_MigrationName.cs
```

#### Ověření migrací před deploymentem:

```bash
# Zobraz seznam migrací
dotnet ef migrations list

# Zkontroluj strukturu
ls -la Migrations/

# Ujisti se:
# 1. Každá .cs migrace má svůj .Designer.cs
# 2. Názvy souborů mají správný formát YYYYMMDDHHMMSS_Name
# 3. Migrace nejsou prázdné (otevři a zkontroluj obsah)
```

---

## 🚀 Deployment proces

### Docker Build

**⚠️ DŮLEŽITÉ:** Vždy používej `--no-cache` pokud měníš migrace nebo kód:

```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

**Důvod:** Docker cache může obsahovat staré verze souborů, což způsobí deployment selhání.

### Multi-stage Dockerfile struktur

```dockerfile
# Stage 1: Build (s .NET SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime (bez SDK - menší image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AAS.Web.dll"]
```

**⚠️ Poznámka:** Runtime image NEMÁ .NET SDK, takže nemůžeš spouštět `dotnet ef` příkazy na productionu!

### Automatické migrace v Program.cs

```csharp
// Toto zajišťuje automatickou aplikaci migrací při startu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```

**⚠️ Pokud jsou migrace neplatné (chybějící Designer, prázdné), aplikace spadne při startu!**

---

## 🗄️ Databázová struktura

### PostgreSQL konfigurace

**Connection String:**
```
Host=db;Database=aasdb;Username=aasuser;Password=aaspassword
```

**Důležité tabulky:**

#### Collections
```sql
Columns:
- Id (PK)
- Name
- Description
- Price (decimal) -- Přidáno v AddPriceStatusVerified
- Status (string) -- Přidáno v AddPriceStatusVerified
- AASVerified (boolean) -- ⚠️ Přidáno MANUÁLNĚ (migrace byla prázdná)
- ...
```

#### BlogPosts
```sql
-- ⚠️ Vytvořeno MANUÁLNĚ (migrace AddCommentsAndBlog byla prázdná)
Columns:
- Id (PK)
- Title
- Content
- AuthorId (FK -> AspNetUsers)
- CreatedAt
- UpdatedAt
- ...
```

#### Comments
```sql
-- ⚠️ Vytvořeno MANUÁLNĚ (migrace AddCommentsAndBlog byla prázdná)
Columns:
- Id (PK)
- CollectionId (FK -> Collections)
- UserId (FK -> AspNetUsers)
- Text
- CreatedAt
- ...
```

### Přístup k databázi

```bash
# Vstup do PostgreSQL kontejneru
docker exec -it <postgres_container_name> psql -U aasuser -d aasdb

# Užitečné SQL příkazy
\dt                    # Seznam tabulek
\d "Collections"       # Struktura tabulky
SELECT * FROM "__EFMigrationsHistory";  # Historie aplikovaných migrací
```

---

## 🔧 Důležité příkazy

### Docker

```bash
# Build s --no-cache (doporučeno při změnách migrací)
docker-compose -f docker-compose.prod.yml build --no-cache

# Start služeb
docker-compose -f docker-compose.prod.yml up -d

# Stop služeb
docker-compose -f docker-compose.prod.yml down

# Zobrazit logy
docker-compose -f docker-compose.prod.yml logs -f web

# Rebuild a restart
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

### Debugging

```bash
# Kontrola běžících kontejnerů
docker ps

# Vstup do web kontejneru
docker exec -it <web_container_name> bash

# Logy aplikace
docker logs <web_container_name> --tail 100 -f

# Vstup do DB
docker exec -it <db_container_name> psql -U aasuser -d aasdb
```

### Entity Framework (v SDK kontejneru)

```bash
# Start SDK kontejneru
docker run -it --rm \
  -v /app:/app \
  -w /app/src/AAS.Web \
  --network aas_default \
  -e ConnectionStrings__DefaultConnection="Host=db;Database=aasdb;Username=aasuser;Password=aaspassword" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash

# V kontejneru:
dotnet tool install --global dotnet-ef --version 8.0.11
export PATH="$PATH:/root/.dotnet/tools"

dotnet ef migrations list
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## 🐛 Known Issues & Solutions

### Issue #1: "Relation 'BlogPosts' does not exist"

**Symptom:** Aplikace spadne při startu s `Npgsql.PostgresException: relation "BlogPosts" does not exist`

**Root Cause:** Migrace `AddCommentsAndBlog` byla prázdná, takže tabulky nebyly vytvořeny.

**Solution:**
1. Zkontroluj obsah migrace `/src/AAS.Web/Migrations/20251117232619_AddCommentsAndBlog.cs`
2. Pokud je prázdná, vytvoř tabulky manuálně (viz SQL výše)
3. Pro dlouhodobé řešení: Vyšetři, proč byla migrace vygenerována prázdná

---

### Issue #2: "No migrations were found"

**Symptom:** EF Core hlásí, že nenašlo žádné migrace, i když existují.

**Possible Causes:**
1. Migrace jsou ve špatné složce (např. `/Database/Migrations/` místo `/Migrations/`)
2. Chybí `.Designer.cs` soubory
3. Nesprávný formát názvů souborů

**Solution:**
1. Zkontroluj, že všechny migrace jsou v `/src/AAS.Web/Migrations/`
2. Ověř, že každá .cs migrace má svůj .Designer.cs
3. Ověř správný formát názvů (YYYYMMDDHHMMSS_Name)

---

### Issue #3: Docker build používá starou verzi kódu

**Symptom:** Změny v kódu se neprojeví po rebuildu.

**Root Cause:** Docker cache obsahuje staré vrstvy.

**Solution:**
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

---

### Issue #4: Package version incompatibilities

**Symptom:** Chyby typu "Package X version Y is not compatible with framework Z"

**Solution:**
1. Zkontroluj verze v `AAS.Web.csproj`
2. Ujisti se, že všechny EF Core balíčky mají stejnou verzi (8.0.11)
3. Použij `dotnet restore --force-evaluate`

---

### Issue #5: Network issues v Docker kontejneru

**Symptom:** SDK kontejner nemůže dosáhnout DB kontejneru.

**Solution:**
```bash
# Přidej --network flag při spuštění:
docker run ... --network aas_default ...

# Nebo zjisti správný network:
docker network ls
```

---

## ✅ Best Practices

### 1. Před přidáním nové migrace

- [ ] Zkontroluj, že máš POUZE jednu složku `Migrations`
- [ ] Ujisti se, že poslední migrace byly aplikovány úspěšně
- [ ] Prověď `dotnet ef migrations list` před přidáním nové

### 2. Po vygenerování migrace

- [ ] Zkontroluj, že byly vytvořeny OBA soubory (.cs + .Designer.cs)
- [ ] Otevři .cs soubor a ověř, že není prázdný
- [ ] Zkontroluj správný formát názvu (YYYYMMDDHHMMSS_Name)
- [ ] Commitni do gitu IHNED (aby se nepřepsaly)

### 3. Před deploymentem

- [ ] Zkontroluj všechny migrace v `/Migrations/` složce
- [ ] Prověř, že žádná není prázdná
- [ ] Build s `--no-cache` pokud měníš migrace
- [ ] Testuj na lokální DB před nasazením

### 4. Po deploymenu

- [ ] Zkontroluj logy aplikace (`docker logs ...`)
- [ ] Ověř, že aplikace běží bez chyb
- [ ] Zkontroluj v DB, že migrace byly aplikovány:
  ```sql
  SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC;
  ```

### 5. Při debuggingu

- [ ] Vždy čti logy od začátku, ne jen poslední řádky
- [ ] Používej `--tail 200` pro delší history
- [ ] Kontroluj ConnectionString v různých prostředích
- [ ] Ověř network connectivity mezi kontejnery

---

## 📝 Pending Tasks (TODO)

### Vysoká priorita
- [ ] **Vyšetřit root cause prázdné migrace AddCommentsAndBlog**
  - Proč EF Core vygenerovalo prázdnou migraci?
  - Zkontrolovat DbContext konfiguraci
  - Ověřit, že DbSet<BlogPost> a DbSet<Comment> jsou správně registrovány

### Střední priorita
- [ ] **Dokončit lokalizaci**
  - Přidat překlady pro Blog a Comments do všech .resx souborů
  - Testovat prepínání jazyků

- [ ] **Implementovat DeepL službu**
  - Získat DeepL API klíč od uživatele
  - Implementovat automatický překlad obsahu
  - Integrovat do blog a comment systému

### Nízká priorita
- [ ] Optimalizovat Docker image size
- [ ] Přidat health check endpoints
- [ ] Zlepšit error handling v controllers

---

## 🔍 Debugging Checklist

Když něco nefunguje, projdi tento checklist:

1. **Aplikace nespadne při startu?**
   ```bash
   docker logs <web_container> --tail 200
   ```

2. **Migrace jsou v pořádku?**
   ```bash
   ls -la /app/src/AAS.Web/Migrations/
   # Zkontroluj: formát názvů, .Designer.cs, nejsou prázdné
   ```

3. **DB je dostupná?**
   ```bash
   docker exec -it <db_container> psql -U aasuser -d aasdb -c "\dt"
   ```

4. **Tabulky existují?**
   ```sql
   SELECT table_name FROM information_schema.tables 
   WHERE table_schema='public';
   ```

5. **Migrace byly aplikovány?**
   ```sql
   SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
   ```

6. **ConnectionString je správný?**
   ```bash
   cat /app/src/AAS.Web/appsettings.Production.json
   ```

---

## 🎓 Lessons Learned

### 1. EF Core migrace jsou zrádné
- Vždy kontroluj, že migrace nejsou prázdné
- .Designer.cs soubory jsou POVINNÉ
- Formát názvů je kritický

### 2. Docker cache může způsobit problémy
- Při změnách migrací vždy `--no-cache`
- Staré vrstvy = staré problémy

### 3. Production debugging je těžký
- Runtime image nemá SDK
- Musíš používat separátní SDK kontejner pro EF tools
- Logy jsou tvůj nejlepší přítel

### 4. Manuální SQL je OK jako hotfix
- Ale není dlouhodobé řešení
- Vždy se vrať a oprav root cause

### 5. Dokumentace je klíčová
- Tento soubor by měl ušetřit hodiny debuggingu
- Aktualizuj ho při každé velké změně

---

## 📞 Kontakt & Podpora

Pokud narazíš na problém, který není v tomto dokumentu:

1. Zkontroluj logy (`docker logs`)
2. Zkontroluj databázi (PostgreSQL console)
3. Zkontroluj migrace (formát, Designer soubory, obsah)
4. Použij troubleshoot_agent pro deep RCA
5. Aktualizuj tento dokument s řešením!

---

**Poslední aktualizace:** 2025-01-17  
**Verze aplikace:** 1.0 (Blog + Comments + AAS Verified)  
**Status:** ✅ Funkční (s manuálními opravami)

---

**⚠️ PŘED ODCHODEM:**
- Přečetl jsi sekci "EF Core - KRITICKÉ POZNATKY"?
- Znáš Deployment proces?
- Víš, jak debugovat migrace?

**Pokud ano, jsi připraven pro další vývoj! 🚀**
