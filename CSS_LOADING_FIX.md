# Oprava načítání CSS - CSS Loading Fix

## Problém / Problem

Po spuštění aplikace se CSS soubory nenačítaly správně:
- ❌ Logo bylo obří (bez CSS omezení velikosti)
- ❌ Texty byly rozteklé bez formátování
- ❌ Layout byl rozbitý (žádné CSS grid/flexbox)
- ❌ Input pole byla úzká, bez stylů
- ❌ Divize nebyly vycentrované

## Příčina / Root Cause

**Nginx konfigurace používala špatný přístup pro servírování static files:**

```nginx
# ❌ ŠPATNĚ - regex location s try_files
location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
    root /app/wwwroot;
    try_files $uri @backend;
    ...
}
```

**Proč to nefungovalo:**
1. Regex location `~*` má nižší prioritu než prefix locations
2. `try_files $uri @backend` může špatně interpretovat cesty
3. Prohlížeč posílá request např. `/css/site.css`, ale Nginx to interpretoval špatně

## Řešení / Solution

**Změna na explicitní location direktivy s `alias`:**

```nginx
# ✅ SPRÁVNĚ - explicitní locations s alias
location /css/ {
    alias /app/wwwroot/css/;
    expires 1y;
    add_header Cache-Control "public, immutable";
    add_header X-Content-Type-Options "nosniff";
    access_log off;
}

location /js/ {
    alias /app/wwwroot/js/;
    ...
}

location /images/ {
    alias /app/wwwroot/images/;
    ...
}
```

**Výhody tohoto přístupu:**
1. ✅ Přesné matchování cest
2. ✅ `alias` správně mapuje `/css/file.css` → `/app/wwwroot/css/file.css`
3. ✅ Vyšší priorita než regex locations
4. ✅ Lepší cache control pro různé typy souborů

## Jak aplikovat opravu / How to Apply Fix

### Metoda 1: Použít automatický fix skript (DOPORUČENO)
```bash
cd /AAS
chmod +x fix-css-and-restart.sh
./fix-css-and-restart.sh
```

### Metoda 2: Manuální restart
```bash
cd /AAS

# Zkopírovat nový nginx.prod.conf (už je v repository)
# Restartovat služby
docker compose -f docker-compose.prod.yml --env-file .env.production down
docker compose -f docker-compose.prod.yml --env-file .env.production up -d

# Ověřit Nginx konfiguraci
docker exec aas-nginx-prod nginx -t

# Restartovat Nginx pro jistotu
docker restart aas-nginx-prod
```

## Ověření / Verification

### 1. Zkontrolovat, že static files jsou v shared volume
```bash
docker exec aas-nginx-prod ls -la /app/wwwroot/
docker exec aas-nginx-prod ls -la /app/wwwroot/css/
docker exec aas-nginx-prod ls -la /app/wwwroot/js/
docker exec aas-nginx-prod ls -la /app/wwwroot/images/
```

Měli byste vidět:
```
/app/wwwroot/
├── css/
│   ├── site.css
│   └── ...
├── js/
│   └── site.js
├── images/
│   ├── logo.png
│   └── ...
├── Identity/
└── robots.txt
```

### 2. Test v prohlížeči
1. Otevřít: `https://aristocraticartworksale.com`
2. Stisknout `F12` (DevTools)
3. Přejít na tab **Network**
4. Stisknout `Ctrl+F5` (hard refresh)
5. Zkontrolovat, že CSS soubory se načítají se **status 200**:
   ```
   /css/site.css        200  text/css
   /js/site.js          200  application/javascript
   /images/logo.png     200  image/png
   ```

### 3. Vizuální kontrola
Po načtení stránky by mělo být vše správně:
- ✅ Logo má normální velikost
- ✅ Texty jsou formátované
- ✅ Layout je správně zarovnaný
- ✅ Input pole mají správnou šířku
- ✅ Barvy a styly se aplikují

### 4. Kontrola Nginx logů
```bash
# Zkontrolovat access log pro CSS requests
docker exec aas-nginx-prod tail -f /var/log/nginx/access.log | grep -E "\.(css|js|png|jpg)"

# Měli byste vidět status 200:
# GET /css/site.css HTTP/1.1" 200
```

## Technické detaily / Technical Details

### Struktura static files v Docker

```
┌─────────────────┐
│   Web Container │
│                 │
│  /app/wwwroot/  │──┐
│  ├── css/       │  │
│  ├── js/        │  │  (docker-entrypoint.sh)
│  └── images/    │  │  cp -r /app/wwwroot/* /shared-static/
└─────────────────┘  │
                     │
                     ▼
              ┌──────────────┐
              │ Docker Volume│
              │ static-files │
              └──────────────┘
                     │
                     │ (mounted as /app/wwwroot)
                     ▼
┌──────────────────────┐
│   Nginx Container    │
│                      │
│  /app/wwwroot/       │
│  ├── css/  ◄─────────┼── location /css/ { alias /app/wwwroot/css/; }
│  ├── js/   ◄─────────┼── location /js/ { alias /app/wwwroot/js/; }
│  └── images/ ◄───────┼── location /images/ { alias /app/wwwroot/images/; }
└──────────────────────┘
```

### Nginx location priority (důležité!)

Nginx vyhodnocuje locations v tomto pořadí:
1. `=` (exact match) - nejvyšší priorita
2. `^~` (prefix match without regex check)
3. `~*` and `~` (regex match) - **nižší priorita**
4. (no modifier) - prefix match

Proto explicitní `location /css/` má vyšší prioritu než `location ~* \.(css)$`

### Cache Control Headers

```nginx
expires 1y;
add_header Cache-Control "public, immutable";
```

- `expires 1y` = prohlížeč cachuje 1 rok
- `public` = může být cachován i proxy servery
- `immutable` = soubor se nikdy nezmění (pro fingerprinted assets)

Pro dev prostředí bychom použili:
```nginx
expires -1;
add_header Cache-Control "no-cache, no-store, must-revalidate";
```

## Troubleshooting

### CSS se stále nenačítá

1. **Hard refresh v prohlížeči:**
   ```
   Windows/Linux: Ctrl + F5
   Mac: Cmd + Shift + R
   ```

2. **Vymazat browser cache:**
   - Chrome: DevTools → Application → Clear storage
   - Firefox: DevTools → Storage → Clear all

3. **Zkontrolovat Nginx error log:**
   ```bash
   docker logs aas-nginx-prod --tail 100 | grep -i error
   ```

4. **Ověřit, že soubory existují:**
   ```bash
   docker exec aas-nginx-prod cat /app/wwwroot/css/site.css | head -20
   ```

5. **Test curl přímo na kontejner:**
   ```bash
   docker exec aas-nginx-prod curl -I http://localhost:80/css/site.css
   # Mělo by vrátit: HTTP/1.1 200 OK
   ```

### Static files se nekopírují

Pokud logy ukazují "Static files not found":

```bash
# Zkontrolovat web container
docker logs aas-web-prod | grep -i static

# Manuálně zkopírovat
docker exec aas-web-prod cp -r /app/wwwroot/* /shared-static/

# Restartovat web container
docker restart aas-web-prod
```

### 404 errors pro static files

```bash
# Zkontrolovat Nginx mount points
docker inspect aas-nginx-prod | grep -A 10 Mounts

# Mělo by ukázat:
# "Source": "aas_static-files"
# "Destination": "/app/wwwroot"
```

## Co dělat dál / Next Steps

1. ✅ Spusťte `./fix-css-and-restart.sh`
2. ✅ Ověřte v prohlížeči, že CSS se načítá (F12 → Network)
3. ✅ Zkontrolujte vizuální vzhled webu
4. ✅ Test všech stránek (Home, About, Contact, Login, Register)

---

**CSS by nyní mělo fungovat správně!** 🎨
