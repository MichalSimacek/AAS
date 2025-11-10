# Oprava HTTP 400 pro statické soubory

## Problém
Statické soubory (CSS, JS, obrázky) vrací HTTP 400 error kvůli chybějícím proxy headers při forwardování z Nginx na ASP.NET Core.

## Řešení
Statické soubory jsou nyní servírovány **přímo z Nginx** místo proxy_pass na ASP.NET Core. To:
- ✅ Eliminuje problém s Host header validací
- ✅ Zvyšuje výkon (Nginx je optimalizován pro statické soubory)
- ✅ Snižuje zátěž na ASP.NET Core aplikaci

## Změny provedené

### 1. Nginx konfigurace (`nginx/nginx.conf` a `nginx/nginx.prod.conf`)
- Změněn `proxy_pass` na `root` direktivu pro statické soubory
- Přidán fallback `@backend` pro dynamicky generované soubory
- Statické soubory jsou servírovány z `/app/wwwroot`

### 2. Docker konfigurace (`docker-compose.prod.yml`)
- Přidán shared volume `static-files` mezi web a nginx kontejnery
- Web kontejner kopíruje wwwroot do `/shared-static` při startu
- Nginx kontejner mountuje tento volume jako read-only

### 3. Dockerfile (`Dockerfile.prod`)
- Přidán entrypoint script (`docker-entrypoint.sh`)
- Script kopíruje statické soubory do shared volume při startu
- Vytvoří `/shared-static` directory

### 4. Entrypoint script (`docker-entrypoint.sh`)
- Kopíruje `/app/wwwroot/*` do `/shared-static/` při startu kontejneru
- Automaticky synchronizuje statické soubory mezi kontejnery

## Nasazení

### Production (Docker Compose)
```bash
# Rebuild a restart služeb
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d

# Kontrola logů
docker-compose -f docker-compose.prod.yml logs -f nginx
docker-compose -f docker-compose.prod.yml logs -f web
```

### Development (bez Nginx)
V development módu (bez Nginx) aplikace běží přímo přes ASP.NET Core Kestrel server a statické soubory jsou servírovány správně. HTTP 400 problém se vyskytuje pouze s Nginx proxy.

## Testování

1. Zkontrolujte, že statické soubory jsou dostupné:
```bash
curl -I https://yourdomain.com/css/site.css
curl -I https://yourdomain.com/js/site.js
curl -I https://yourdomain.com/images/logo.png
```

2. Měli byste vidět:
```
HTTP/2 200
content-type: text/css (nebo application/javascript, image/png)
cache-control: public, immutable
```

3. Zkontrolujte Nginx error log:
```bash
docker exec aas-nginx-prod tail -f /var/log/nginx/error.log
```

## Řešení problémů

### Statické soubory stále vracejí 404
- Zkontrolujte, že shared volume je správně namountován:
  ```bash
  docker exec aas-nginx-prod ls -la /app/wwwroot/
  ```
- Měli byste vidět css/, js/, images/ složky

### Statické soubory vracejí stále 400
- To by se již nemělo stát, protože Nginx je servíruje přímo
- Pokud ano, zkontrolujte Nginx logs pro detaily

### Po rebuild nejsou statické soubory dostupné
- Entrypoint script možná selhal
- Zkontrolujte logy web kontejneru:
  ```bash
  docker-compose -f docker-compose.prod.yml logs web | grep "Copying static"
  ```

## Další vylepšení (volitelné)

1. **Přidat checksums**: Pro zajištění, že soubory jsou správně zkopírovány
2. **Použít multi-stage sync**: Při každém deployu automaticky aktualizovat statické soubory
3. **CDN integrace**: Pro globální distribuci statických souborů

## Poznámky

- Upload složka (`/var/www/uploads/`) je stále servírována z bind mount
- Dynamický obsah prochází přes ASP.NET Core
- Toto řešení je optimální pro Docker production environment
