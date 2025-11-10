# 📋 Index opravy HTTP 400 - Statické soubory

## 🎯 Rychlý přehled

| Soubor | Stav | Popis |
|--------|------|-------|
| `nginx/nginx.conf` | ✅ Upraven | Nginx dev konfigurace - servírování statických souborů přímo |
| `nginx/nginx.prod.conf` | ✅ Upraven | Nginx prod konfigurace - servírování statických souborů přímo |
| `docker-compose.prod.yml` | ✅ Upraven | Přidán shared volume pro statické soubory |
| `Dockerfile.prod` | ✅ Upraven | Přidán entrypoint script pro kopírování statických souborů |
| `docker-entrypoint.sh` | ✅ Nový | Script pro kopírování wwwroot do shared volume |
| `STATIC_FILES_FIX.md` | ✅ Nový | Detailní dokumentace opravy |
| `CHANGES_SUMMARY.md` | ✅ Nový | Souhrn všech změn |
| `test-static-files.sh` | ✅ Nový | Test script pro ověření funkčnosti |
| `QUICK_FIX_COMMANDS.sh` | ✅ Nový | Automatický deployment script |
| `FIX_INDEX.md` | ✅ Nový | Tento soubor - index všech změn |

## 📚 Dokumentace

### Hlavní dokumenty
1. **[CHANGES_SUMMARY.md](CHANGES_SUMMARY.md)** - Kompletní souhrn problému a řešení
2. **[STATIC_FILES_FIX.md](STATIC_FILES_FIX.md)** - Detailní technická dokumentace
3. **[FIX_INDEX.md](FIX_INDEX.md)** - Tento soubor - rychlá navigace

### Scripty
1. **[QUICK_FIX_COMMANDS.sh](QUICK_FIX_COMMANDS.sh)** - Automatický deployment
2. **[test-static-files.sh](test-static-files.sh)** - Test statických souborů
3. **[docker-entrypoint.sh](docker-entrypoint.sh)** - Entrypoint pro Docker

## 🚀 Quick Start

### Pro okamžité nasazení:
```bash
# Spustit automatický deployment
./QUICK_FIX_COMMANDS.sh
```

### Pro manuální nasazení:
```bash
# 1. Zastavit kontejnery
docker-compose -f docker-compose.prod.yml down

# 2. Rebuild
docker-compose -f docker-compose.prod.yml build --no-cache

# 3. Spustit
docker-compose -f docker-compose.prod.yml up -d

# 4. Testovat
./test-static-files.sh yourdomain.com
```

## 🔍 Troubleshooting

### Problém: Statické soubory stále vracejí 400

**Řešení 1:** Zkontrolovat shared volume
```bash
docker exec aas-nginx-prod ls -la /app/wwwroot/
```

**Řešení 2:** Zkontrolovat web kontejner logy
```bash
docker logs aas-web-prod | grep "Static"
```

**Řešení 3:** Zkontrolovat Nginx error log
```bash
docker exec aas-nginx-prod tail -f /var/log/nginx/error.log
```

### Problém: Statické soubory nejsou v Nginx kontejneru

**Řešení:** Zkontrolovat entrypoint script
```bash
docker logs aas-web-prod --tail=50 | grep "Copying static"
```

### Problém: 404 místo 400

**Odpověď:** To je lepší! 404 znamená, že Nginx je servíruje, ale soubor nebyl nalezen. Zkontrolujte cestu.

## 📊 Technické detaily

### Architektura před opravou:
```
Request → Nginx → proxy_pass (BEZ headers) → ASP.NET Core → 400 Error
```

### Architektura po opravě:
```
Request → Nginx → Přímé servírování z /app/wwwroot → 200 OK
                                                   ↓
                                      (fallback) ASP.NET Core
```

### Jak funguje shared volume:
```
1. Docker vytvoří named volume "static-files"
2. Web kontejner při startu:
   - Zkopíruje /app/wwwroot/* do /shared-static/
   - Volume je namountován na /shared-static/
3. Nginx kontejner:
   - Mountuje stejný volume na /app/wwwroot/
   - Servíruje soubory přímo
```

## 🎓 Pro hlubší pochopení

### Přečíst v tomto pořadí:
1. **CHANGES_SUMMARY.md** - Pro celkový přehled změn
2. **STATIC_FILES_FIX.md** - Pro deployment instrukce
3. Kód v souborech - Pro implementační detaily

### Klíčové koncepty:
- **Proxy headers** - Host, X-Real-IP, X-Forwarded-For, etc.
- **ForwardedHeaders middleware** - ASP.NET Core očekává tyto headers
- **Nginx root directive** - Servírování souborů přímo z filesystému
- **try_files** - Fallback mechanismus
- **Named volumes** - Sdílení dat mezi Docker kontejnery

## ✅ Checklist po nasazení

- [ ] Kontejnery běží (`docker ps`)
- [ ] Statické soubory jsou v Nginx (`docker exec aas-nginx-prod ls /app/wwwroot/`)
- [ ] CSS vrací 200 (`curl -I https://domain.com/css/site.css`)
- [ ] JS vrací 200 (`curl -I https://domain.com/js/site.js`)
- [ ] Obrázky vracejí 200 (`curl -I https://domain.com/images/logo.png`)
- [ ] Žádné chyby v Nginx logs
- [ ] Aplikace funguje normálně

## 📞 Potřebujete pomoct?

1. **Spustit test script:**
   ```bash
   ./test-static-files.sh yourdomain.com
   ```

2. **Zkontrolovat logy:**
   ```bash
   docker-compose -f docker-compose.prod.yml logs -f
   ```

3. **Přečíst dokumentaci:**
   - CHANGES_SUMMARY.md pro přehled
   - STATIC_FILES_FIX.md pro detaily

4. **Debug mode:**
   ```bash
   # Interaktivní shell v Nginx
   docker exec -it aas-nginx-prod sh
   
   # Interaktivní shell ve Web
   docker exec -it aas-web-prod bash
   ```

## 🎉 Hotovo!

Po úspěšném nasazení byste měli vidět:
- ✅ HTTP 200 pro všechny statické soubory
- ✅ Žádné 400 chyby v logách
- ✅ Rychlejší načítání stránky
- ✅ Nižší zátěž ASP.NET Core

---

**Vytvořeno:** 2025
**Účel:** Oprava HTTP 400 pro statické soubory v AAS aplikaci
**Status:** ✅ Připraveno k nasazení
