# 🚀 Quick Start BEZ Dockeru

## Situace: Nemám Docker Desktop nebo nechci ho používat

Máte **2 možnosti**:

---

## ✅ Možnost 1: Nainstalovat Docker Desktop (DOPORUČENO)

### Proč Docker?
- ✅ Jednoduché - PostgreSQL běží v kontejneru
- ✅ Čisté - neinstaluješ PostgreSQL na systém
- ✅ Rychlé - jeden příkaz a máš DB ready
- ✅ Izolované - neovlivní ostatní aplikace

### Jak nainstalovat:

1. **Stáhni Docker Desktop:**
   ```
   https://www.docker.com/products/docker-desktop
   ```

2. **Nainstaluj a spusť** Docker Desktop

3. **Počkej** až se Docker spustí (ikona v system tray)

4. **Restartuj VS Code**

5. **Spusť PostgreSQL:**
   ```powershell
   docker-compose -f docker-compose.dev.yml up -d postgres
   ```

6. **Stiskni F5** ve VS Code

---

## ✅ Možnost 2: Použít lokální PostgreSQL (bez Dockeru)

### Instalace PostgreSQL na Windows

1. **Stáhni PostgreSQL 16:**
   ```
   https://www.postgresql.org/download/windows/
   ```

2. **Nainstaluj** s těmito údaji:
   - Port: `5432`
   - Superuser: `postgres`
   - Password: `něco bezpečného`

3. **Vytvoř development databázi:**

   Otevři SQL Shell (psql) a zadej:
   ```sql
   -- Vytvoř uživatele
   CREATE USER aas_dev WITH PASSWORD 'dev_password_123';

   -- Vytvoř databázi
   CREATE DATABASE aas_dev OWNER aas_dev;

   -- Dej oprávnění
   GRANT ALL PRIVILEGES ON DATABASE aas_dev TO aas_dev;
   ```

4. **Connection string je už správně nastavený!**

   V `appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=aas_dev;Username=aas_dev;Password=dev_password_123;Pooling=true"
   }
   ```

5. **Použij launch konfiguraci BEZ auto-startu:**

   Ve VS Code debug dropdown vyber:
   ```
   🚀 Launch Web (Manual PostgreSQL)
   ```

6. **Stiskni F5**

---

## 🎮 Jak spustit aplikaci BEZ Dockeru

### Krok za krokem:

1. **Ujisti se, že PostgreSQL běží:**

   **Pokud máš Docker:**
   ```powershell
   docker-compose -f docker-compose.dev.yml up -d postgres
   ```

   **Pokud máš lokální PostgreSQL:**
   - Služba "PostgreSQL" by měla běžet v Services (Win+R → `services.msc`)

2. **Otevři VS Code:**
   ```powershell
   code .
   ```

3. **Vyber správnou launch konfiguraci:**

   Klikni na dropdown vedle tlačítka Run (nebo F5):

   - **Máš Docker?** → `🚀 Launch Web (Auto-start PostgreSQL)`
   - **Nemáš Docker?** → `🚀 Launch Web (Manual PostgreSQL)` ⭐

4. **Stiskni F5**

5. **Browser otevře** `http://localhost:5000`

---

## 🐛 Troubleshooting

### "password authentication failed for user aas_dev"

**Problém:** PostgreSQL neběží nebo má jiné heslo

**Řešení:**

**Pokud máš Docker:**
```powershell
# Zastav kontejner
docker-compose -f docker-compose.dev.yml down

# Smaž volume (resetuje heslo)
docker volume rm aas_postgres_dev_data

# Spusť znovu
docker-compose -f docker-compose.dev.yml up -d postgres
```

**Pokud máš lokální PostgreSQL:**
```sql
-- Připoj se jako postgres superuser
-- V SQL Shell (psql):
ALTER USER aas_dev WITH PASSWORD 'dev_password_123';
```

### "Docker is not installed or not in PATH"

**Problém:** Docker není dostupný v PowerShell

**Řešení:**

**Možnost A:** Nainstaluj Docker Desktop (viz výše)

**Možnost B:** Použij lokální PostgreSQL (viz výše)

**Možnost C:** Přidej Docker do PATH:
1. Najdi cestu k Docker: `C:\Program Files\Docker\Docker\resources\bin`
2. Přidej do PATH environment variable
3. Restartuj VS Code

### "port 5432 is already in use"

**Problém:** Něco už běží na portu 5432

**Zjisti co to je:**
```powershell
netstat -ano | findstr :5432
```

**Možnost A:** Máš lokální PostgreSQL
- Použij ji místo Dockeru (viz "Možnost 2" výše)

**Možnost B:** Máš starý Docker kontejner
```powershell
docker ps -a
docker stop aas_dev_postgres
docker rm aas_dev_postgres
```

### "Cannot connect to database"

**Zkontroluj že PostgreSQL běží:**

**Docker:**
```powershell
docker ps | findstr postgres
```

**Lokální:**
- Win+R → `services.msc`
- Najdi "PostgreSQL"
- Status = "Running"

---

## 📋 Porovnání: Docker vs Lokální PostgreSQL

| Feature | Docker | Lokální PostgreSQL |
|---------|--------|-------------------|
| **Instalace** | Jednoduché | Složitější |
| **Čištění systému** | ✅ Neovlivní systém | ❌ Instaluje služby |
| **Rychlost startu** | ~5-10 sec | Instant |
| **Izolace** | ✅ Plná izolace | ❌ Sdílená s ostatními |
| **Reset DB** | Snadné (smaž volume) | Složitější |
| **Velikost** | ~600MB (Docker) | ~200MB |

---

## ✅ Doporučení

### Pokud jste vývojář:
→ **Použijte Docker** - Je to standard pro moderní vývoj

### Pokud nemůžete nainstalovat Docker:
→ **Použijte lokální PostgreSQL** - Funguje stejně dobře

### Pokud máte oba:
→ **Docker pro development, lokální pro testing**

---

## 🎯 Shrnutí

**BEZ Dockeru můžete normálně vyvíjet!**

Stačí:
1. Nainstalovat PostgreSQL lokálně
2. Vytvořit databázi `aas_dev` s uživatelem `aas_dev`
3. Použít launch konfiguraci: **"🚀 Launch Web (Manual PostgreSQL)"**
4. Stisknout F5

**Connection string už je správně nastavený v `appsettings.Development.json`!**

---

*Pro další pomoc viz: QUICK_START.md, DEVELOPMENT.md*
