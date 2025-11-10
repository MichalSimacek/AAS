# Souhrn změn - Oprava HTTP 400 pro statické soubory

## 🔍 Identifikovaný problém

**HTTP 400 chyba** pro všechny statické soubory (CSS, JS, obrázky) způsobená:
- Nginx forwadoval statické soubory přes `proxy_pass` na ASP.NET Core
- **Chyběly důležité proxy headers** (Host, X-Real-IP, X-Forwarded-For)
- ASP.NET Core `ForwardedHeaders` middleware očekává tyto headers
- Bez nich ASP.NET Core odmítl požadavky s HTTP 400

## ✅ Implementované řešení

Statické soubory jsou nyní **servírovány přímo z Nginx** místo forwardování na ASP.NET Core.

### Výhody tohoto řešení:
1. ✅ **Řeší HTTP 400 problém** - žádné proxy headers nejsou potřeba
2. ✅ **Lepší výkon** - Nginx je optimalizován pro servírování statických souborů
3. ✅ **Nižší zátěž** - ASP.NET Core se může soustředit na business logiku
4. ✅ **Lepší caching** - Nginx má efektivnější cache mechanismy

## 📝 Změněné soubory

### 1. `/app/nginx/nginx.conf`
**Před:**
```nginx
location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
    proxy_pass http://aas_app;  # ❌ Forwadování na ASP.NET Core
    expires 1y;
    add_header Cache-Control "public, immutable";
    access_log off;
}
```

**Po:**
```nginx
location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
    root /app/wwwroot;          # ✅ Servírování přímo z Nginx
    try_files $uri @backend;    # ✅ Fallback pro dynamické soubory
    expires 1y;
    add_header Cache-Control "public, immutable";
    access_log off;
}

location @backend {             # ✅ Fallback s korektními headers
    proxy_pass http://aas_app;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    # ... další headers
}
```

### 2. `/app/nginx/nginx.prod.conf`
Stejné změny jako v `nginx.conf`

### 3. `/app/docker-compose.prod.yml`
**Přidáno:**
- Shared volume `static-files` mezi web a nginx kontejnery
- Web kontejner: `- static-files:/shared-static`
- Nginx kontejner: `- static-files:/app/wwwroot:ro`

### 4. `/app/Dockerfile.prod`
**Přidáno:**
- Kopírování `docker-entrypoint.sh`
- Vytvoření `/shared-static` directory
- Změna ENTRYPOINT na `/docker-entrypoint.sh`

### 5. `/app/docker-entrypoint.sh` (NOVÝ)
Entrypoint script který:
- Kopíruje `/app/wwwroot/*` do `/shared-static/` při startu
- Loguje kopírování pro debugging
- Spouští ASP.NET Core aplikaci

### 6. `/app/STATIC_FILES_FIX.md` (NOVÝ)
Detailní dokumentace opravy a deployment instrukce

### 7. `/app/test-static-files.sh` (NOVÝ)
Test script pro ověření funkčnosti

## 🚀 Jak nasadit změny

### Krok 1: Zastavit běžící kontejnery
```bash
docker-compose -f docker-compose.prod.yml down
```

### Krok 2: Rebuild s čistým cache
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

### Krok 3: Spustit služby
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Krok 4: Ověřit logy
```bash
docker-compose -f docker-compose.prod.yml logs -f web | grep "Static files"
docker-compose -f docker-compose.prod.yml logs -f nginx
```

### Krok 5: Testovat
```bash
# Základní test
curl -I https://yourdomain.com/css/site.css

# Nebo použít test script
./test-static-files.sh yourdomain.com
```

## 🧪 Testování

### Očekávané výsledky:
```bash
$ curl -I https://yourdomain.com/css/site.css
HTTP/2 200
content-type: text/css
cache-control: public, immutable
x-content-type-options: nosniff
# ... další security headers
```

### ❌ Před opravou:
```bash
HTTP/2 400 Bad Request
```

### ✅ Po opravě:
```bash
HTTP/2 200 OK
```

## 📊 Architektura toku

### Před (❌ problematické):
```
Client → Nginx → [proxy_pass bez headers] → ASP.NET Core → HTTP 400
```

### Po (✅ funkční):
```
Client → Nginx → [servírování přímo z /app/wwwroot] → HTTP 200
                                                      ↓
                                         (fallback) ASP.NET Core
```

## 🔐 Bezpečnost

Všechny security headers zůstávají zachované:
- ✅ X-Content-Type-Options: nosniff
- ✅ X-Frame-Options: DENY
- ✅ Strict-Transport-Security
- ✅ Content-Security-Policy
- ✅ Referrer-Policy

## 📈 Výkonnostní benefity

1. **Rychlejší response time** - Nginx servíruje statické soubory rychleji než ASP.NET Core
2. **Nižší CPU usage** - ASP.NET Core nemusí zpracovávat statické požadavky
3. **Lepší škálovatelnost** - Více requestů zvládne stejný hardware
4. **Efektivnější caching** - Nginx má optimalizované cache mechanismy

## 🔧 Troubleshooting

Pokud statické soubory stále nefungují, zkontrolujte:

1. **Volume mounting:**
   ```bash
   docker exec aas-nginx-prod ls -la /app/wwwroot/
   ```

2. **Web kontejner logy:**
   ```bash
   docker logs aas-web-prod | grep "Static"
   ```

3. **Nginx error log:**
   ```bash
   docker exec aas-nginx-prod tail -f /var/log/nginx/error.log
   ```

4. **Použijte test script:**
   ```bash
   ./test-static-files.sh yourdomain.com
   ```

## 📞 Podpora

Pokud problém přetrvává:
1. Zkontrolujte všechny logy výše
2. Ověřte, že Docker volumes jsou správně vytvořeny
3. Zkuste rebuild s `--no-cache`
4. Kontaktujte podporu s logy z kroků výše
