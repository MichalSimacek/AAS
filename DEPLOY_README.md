# Rychlý Deployment Guide

## Předpoklady na vašem serveru

✅ Docker nainstalován  
✅ PostgreSQL běží na hostiteli (localhost:5432)  
✅ ProtonMail Bridge běží na hostiteli (127.0.0.1:1025)  
✅ Doména aristocraticartworksale.com ukazuje na IP serveru  

## Automatický Deployment v 3 krocích

### Krok 1: Nastavte .env.production

```bash
cd /AAS
nano .env.production
```

**Zkopírujte toto:**

```bash
# Database Configuration (Host PostgreSQL)
DB_HOST=host.docker.internal
DB_PORT=5432
DB_NAME=aas
DB_USER=aas
DB_PASSWORD=24baacb39Po9

# Admin Configuration
ADMIN_EMAIL=admin@localhost
ADMIN_PASSWORD=Admin123!@#$

# Email Configuration (ProtonMail Bridge on Host)
EMAIL_SMTP_HOST=host.docker.internal
EMAIL_SMTP_PORT=1025
EMAIL_USE_STARTTLS=false
EMAIL_USERNAME=Michalsimacek@protonmail.com
EMAIL_PASSWORD=u-J2tj_m8wNfL1WDYlMjHg
EMAIL_FROM=noreply@aristocraticartworksale.com
EMAIL_TO=inquiry@aristocraticartworksale.com

# Translation Configuration
TRANSLATION_ENABLED=false
```

**Uložte (Ctrl+O, Enter, Ctrl+X)**

### Krok 2: Spusťte deployment skript

```bash
./deploy.sh
```

Skript automaticky:
- ✅ Zkontroluje předpoklady
- ✅ Zastaví staré kontejnery
- ✅ Ověří konfiguraci
- ✅ Upraví docker-compose pro hostitelskou databázi
- ✅ Zkontroluje DNS
- ✅ Získá SSL certifikáty od Let's Encrypt
- ✅ Sestaví a spustí aplikaci

### Krok 3: Přístup k aplikaci

🌐 **URL**: https://aristocraticartworksale.com  
👤 **Admin**: admin@localhost / Admin123!@#$

## Co skript dělá

### Automatické úpravy:

1. **Zakomentuje PostgreSQL kontejner** v docker-compose.production.yml (používáte hostitelskou DB)
2. **Získá Let's Encrypt certifikáty** automaticky
3. **Nastaví Nginx** jako reverse proxy s HTTPS
4. **Spustí aplikaci** s připojením k hostitelským službám

### Struktura po nasazení:

```
Internet (80/443)
    ↓
Nginx Container (reverse proxy + SSL)
    ↓
ASP.NET Core Container (port 8080)
    ↓
Host PostgreSQL (host.docker.internal:5432)
Host ProtonMail Bridge (host.docker.internal:1025)
```

## Řešení problémů

### Port 80/443 je obsazený

```bash
sudo systemctl stop nginx apache2
./deploy.sh
```

### DNS není nakonfigurované

Počkejte, až DNS propaguje (2-48 hodin), nebo použijte volbu "3) Skip SSL" pro testování

### Aplikace neběží

```bash
# Zobrazit logy
docker-compose -f docker-compose.production.yml logs -f web

# Restartovat
docker-compose -f docker-compose.production.yml restart web
```

### Databázové připojení selhává

Ověřte, že PostgreSQL běží na hostiteli:
```bash
sudo systemctl status postgresql
psql -h localhost -U aas -d aas -c "SELECT version();"
```

### ProtonMail Bridge nefunguje

Ověřte, že běží:
```bash
ps aux | grep proton
telnet 127.0.0.1 1025
```

## Užitečné příkazy

```bash
# Zobrazit logy
docker-compose -f docker-compose.production.yml logs -f

# Restartovat služby
docker-compose -f docker-compose.production.yml restart

# Zastavit vše
docker-compose -f docker-compose.production.yml down

# Aktualizovat aplikaci
git pull origin main
./deploy.sh

# Zobrazit běžící kontejnery
docker-compose -f docker-compose.production.yml ps

# Sledovat logy aplikace
docker-compose -f docker-compose.production.yml logs -f web
```

## Poznámky

- ✅ Certifikáty se automaticky obnovují každých 12 hodin
- ✅ Aplikace se automaticky restartuje při pádu (restart: unless-stopped)
- ✅ Data jsou perzistentní (uploads v /mnt/data/uploads, databáze na hostiteli)
- ✅ Gold design je aplikován (site-new.css)
- ✅ Všechny bezpečnostní hlavičky jsou aktivní

## Testování SSL

Po nasazení ověřte SSL:
- https://www.ssllabs.com/ssltest/analyze.html?d=aristocraticartworksale.com

Očekávaný výsledek: **A nebo A+ rating**

---

**Deployment by měl trvat 5-10 minut celkem** 🚀
