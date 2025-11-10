# Oprava databázového připojení - Database Connection Fix

## Problém / Problem
Aplikace se nemohla připojit k PostgreSQL databázi s chybou:
```
System.Net.Sockets.SocketException: Name or service not known
```

## Příčina / Root Cause
1. **Chyběl soubor `.env.production`** - Docker Compose neměl odkud načíst environment variables
2. **Chyběl health check** - Web kontejner startoval před tím, než byla databáze připravená
3. **Špatná konfigurace emailu** - `EMAIL_SMTP_HOST=127.0.0.1` nefunguje z Docker kontejneru

## Provedené změny / Changes Made

### 1. Vytvořen soubor `.env.production`
✅ Soubor obsahuje všechny potřebné environment variables pro:
- Databázové připojení (DB_HOST=db, DB_PASSWORD, atd.)
- Admin účet
- Email konfiguraci (opraveno na `host.docker.internal`)
- Domain nastavení

### 2. Aktualizován `docker-compose.prod.yml`
✅ Přidán **health check** pro PostgreSQL:
```yaml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d ${DB_NAME}"]
  interval: 5s
  timeout: 5s
  retries: 5
```

✅ Web service nyní čeká na healthy databázi:
```yaml
depends_on:
  db:
    condition: service_healthy
```

✅ Přidán **extra_hosts** pro přístup k host serveru (ProtonMail Bridge):
```yaml
extra_hosts:
  - "host.docker.internal:host-gateway"
```

### 3. Vytvořen restart skript
✅ Nový soubor: `restart-deployment.sh`
- Automaticky načte `.env.production`
- Zastaví staré kontejnery
- Vymaže staré volumes (pro čistý start)
- Spustí služby s health checks
- Čeká na databázi
- Zobrazí logy

## Jak restartovat aplikaci / How to Restart

### Metoda 1: Použijte nový restart skript (DOPORUČENO)
```bash
cd /AAS
chmod +x restart-deployment.sh
./restart-deployment.sh
```

### Metoda 2: Manuální restart
```bash
cd /AAS

# Zastavit a vyčistit
docker compose -f docker-compose.prod.yml --env-file .env.production down
docker volume rm aas_postgres-data

# Spustit znovu
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build

# Sledovat logy
docker logs -f aas-web-prod
```

## Ověření / Verification

Po restartu zkontrolujte:

```bash
# 1. Zkontrolovat health status databáze
docker inspect --format='{{.State.Health.Status}}' aas-db-prod
# Mělo by zobrazit: healthy

# 2. Zkontrolovat, že web service běží
docker ps | grep aas-web-prod
# Měl by být "Up" a ne "Restarting"

# 3. Zkontrolovat logy web aplikace
docker logs aas-web-prod --tail 50
# Měli byste vidět: "Starting ASP.NET Core application" bez erroru

# 4. Zkontrolovat, že aplikace reaguje
curl http://localhost:5000
# Mělo by vrátit HTML
```

## Důležité poznámky / Important Notes

### Email konfigurace
❗ **ProtonMail Bridge na hostu**
- Původní konfigurace: `EMAIL_SMTP_HOST=127.0.0.1` ❌
- Nová konfigurace: `EMAIL_SMTP_HOST=host.docker.internal` ✅

Toto zajišťuje, že web kontejner může komunikovat s ProtonMail Bridge běžícím na host serveru.

### Databázové připojení
✅ `DB_HOST=db` je správně - odkazuje na PostgreSQL kontejner v Docker network
✅ `depends_on` s health check zajišťuje, že databáze je ready před startem web aplikace

## Troubleshooting

### Pokud databáze stále nefunguje:
```bash
# Zkontrolovat databázové logy
docker logs aas-db-prod

# Zkontrolovat network
docker network inspect aas_aas-network

# Zkontrolovat environment variables v kontejneru
docker exec aas-web-prod env | grep DB_
```

### Pokud web aplikace crashuje:
```bash
# Detailní logy
docker logs aas-web-prod --tail 100

# Ověřit, že .env.production je načtený
docker exec aas-web-prod env
```

## Co dělat dál / Next Steps

1. ✅ Spusťte `./restart-deployment.sh`
2. ✅ Ověřte, že aplikace běží bez errorů
3. ✅ Otestujte přístup na `https://aristocraticartworksale.com`
4. ⚠️  Zkontrolujte, že email funguje (registrace, reset hesla)

---

**Připraveno k nasazení!** 🚀
