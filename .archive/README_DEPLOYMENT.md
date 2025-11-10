# Aristocratic Artwork Sale - Production Deployment

## Quick Start

### 1. První nasazení

```bash
cd /AAS

# Ujistěte se, že .env.production existuje a je správně nakonfigurován
nano .env.production

# Nasaďte aplikaci (clean install)
./deploy-production.sh --rebuild --clean
```

### 2. Běžný restart

```bash
cd /AAS
./deploy-production.sh
```

### 3. Rebuild po změnách kódu

```bash
cd /AAS
./deploy-production.sh --rebuild
```

## Konfigurace (.env.production)

```env
# Database
DB_HOST=db
DB_PORT=5432
DB_NAME=aas
DB_USER=aas
DB_PASSWORD=<silné-heslo>

# Admin
ADMIN_EMAIL=admin@localhost
ADMIN_PASSWORD=<silné-heslo-min-12-znaků>

# Email (ProtonMail Bridge na hostu)
EMAIL_SMTP_HOST=host.docker.internal
EMAIL_SMTP_PORT=1025
EMAIL_USE_STARTTLS=false
EMAIL_USERNAME=<your-email>
EMAIL_PASSWORD=<your-password>
EMAIL_FROM=noreply@aristocraticartworksale.com
EMAIL_TO=inquiry@aristocraticartworksale.com

# Domain
DOMAIN=aristocraticartworksale.com
SSL_EMAIL=<your-email>
```

## Řešení problémů

### CSS se nenačítá správně

**Příčina:** Používá se neúplný `site-new.css` místo kompletního `site.css`

**Řešení:**
```bash
./fix-css-use-correct-file.sh
```

### Login/Register stránky vracejí 404

**Příčina:** Nginx měl `/Identity/` nakonfigurované jako static files místo proxy

**Řešení:** Již opraveno v `nginx.prod.conf` - `/Identity/` nyní proxy na backend

### Databáze se nepřipojuje

**Kontrola:**
```bash
# Zkontrolovat .env.production
cat .env.production | grep DB_

# Zkontrolovat database logs
docker logs aas-db-prod --tail 50

# Zkontrolovat web logs
docker logs aas-web-prod --tail 50
```

### Container se neustále restartuje

```bash
# Zkontrolovat logs
docker logs aas-web-prod --tail 100

# Zkontrolovat status
docker ps -a | grep aas-

# Zkontrolovat health
docker inspect aas-db-prod | grep -A 10 Health
```

## Struktura služeb

```
┌─────────────────────────────────────────┐
│   Nginx (Reverse Proxy + SSL)          │
│   Ports: 80, 443                        │
│   Container: aas-nginx-prod             │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│   ASP.NET Core Application              │
│   Port: 5000 (internal)                 │
│   Container: aas-web-prod               │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│   PostgreSQL Database                   │
│   Port: 5432 (internal only)            │
│   Container: aas-db-prod                │
└─────────────────────────────────────────┘
```

## Příkazy pro správu

### Zobrazit logy
```bash
docker logs -f aas-web-prod      # Web application
docker logs -f aas-db-prod       # Database
docker logs -f aas-nginx-prod    # Nginx
```

### Restart konkrétní služby
```bash
docker restart aas-web-prod
docker restart aas-nginx-prod
```

### Vstup do containeru
```bash
docker exec -it aas-web-prod sh
docker exec -it aas-db-prod sh
docker exec -it aas-nginx-prod sh
```

### Kontrola databáze
```bash
docker exec -it aas-db-prod psql -U aas -d aas
```

### Záloha databáze
```bash
docker exec aas-db-prod pg_dump -U aas aas > backup_$(date +%Y%m%d).sql
```

### Obnovení databáze
```bash
cat backup_20251110.sql | docker exec -i aas-db-prod psql -U aas -d aas
```

## Opravené problémy

✅ **Databázové připojení** - Přidán health check, správné depends_on
✅ **CSS načítání** - Změněno z site-new.css na site.css  
✅ **Identity 404** - Nginx proxy pro /Identity/ stránky
✅ **ProtonMail Bridge** - Použití host.docker.internal
✅ **Static files** - Správná Nginx konfigurace s alias

## Dokumentace

- `CSS_FILE_FIX_SUMMARY.md` - Řešení CSS problému
- `FIX_DATABASE_CONNECTION.md` - Řešení DB problému
- `DIRECTORY_STRUCTURE.md` - Kompletní struktura projektu
- `DEPLOYMENT_CHECKLIST.md` - Kontrolní seznam pro nasazení

## Support

Pro další pomoc kontaktujte support nebo konzultujte dokumentaci v repository.
