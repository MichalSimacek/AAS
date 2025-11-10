# 🎯 HTTP 400 Fix - Quick Reference Card

## 🔥 Problém
```
Všechny statické soubory (CSS, JS, obrázky) → HTTP 400
```

## ✅ Řešení
```
Nginx nyní servíruje statické soubory PŘÍMO místo proxy_pass
```

---

## 🚀 Nasazení v 3 krocích

```bash
# 1️⃣ Automaticky (doporučeno)
./QUICK_FIX_COMMANDS.sh

# NEBO 2️⃣ Manuálně
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d

# 3️⃣ Test
./test-static-files.sh yourdomain.com
```

---

## 📝 Co bylo změněno

| Soubor | Změna |
|--------|-------|
| `nginx/nginx.conf` | `proxy_pass` → `root /app/wwwroot` |
| `nginx/nginx.prod.conf` | `proxy_pass` → `root /app/wwwroot` |
| `docker-compose.prod.yml` | Přidán shared volume `static-files` |
| `Dockerfile.prod` | Přidán entrypoint script |
| `docker-entrypoint.sh` | Kopíruje wwwroot do shared volume |

---

## 🧪 Rychlý test

```bash
# Očekávaný výsledek: HTTP 200
curl -I https://yourdomain.com/css/site.css
curl -I https://yourdomain.com/js/site.js
```

---

## 🔍 Troubleshooting

### ❌ Stále 400?
```bash
# Check Nginx logs
docker exec aas-nginx-prod tail /var/log/nginx/error.log
```

### ❌ 404 místo 400?
```bash
# Check if files exist in Nginx
docker exec aas-nginx-prod ls -la /app/wwwroot/
```

### ❌ Prázdná složka v Nginx?
```bash
# Check web container logs
docker logs aas-web-prod | grep "Static"
```

---

## 📚 Dokumentace

| Soubor | Obsah |
|--------|-------|
| `FIX_INDEX.md` | 📋 Index všeho |
| `CHANGES_SUMMARY.md` | 📖 Detailní souhrn změn |
| `STATIC_FILES_FIX.md` | 🔧 Technická dokumentace |

---

## ⚡ One-liner příkazy

```bash
# Rebuild vše
docker-compose -f docker-compose.prod.yml up -d --build --force-recreate

# Ukázat logy
docker-compose -f docker-compose.prod.yml logs -f

# Test všeho
./test-static-files.sh yourdomain.com

# Check Nginx config
docker exec aas-nginx-prod nginx -t

# Restart pouze Nginx
docker-compose -f docker-compose.prod.yml restart nginx
```

---

## ✨ Výhody řešení

- ✅ **Opraveno HTTP 400** - hlavní problém vyřešen
- ⚡ **Rychlejší** - Nginx servíruje statiku efektivněji
- 💪 **Výkonnější** - ASP.NET Core má méně práce
- 🔒 **Bezpečnější** - Všechny security headers zachované

---

## 📞 Potřebuješ pomoct?

1. Spusť: `./test-static-files.sh yourdomain.com`
2. Přečti: `FIX_INDEX.md`
3. Zkontroluj logy: `docker-compose -f docker-compose.prod.yml logs`

---

**Status:** ✅ Připraveno k nasazení  
**Testováno:** ✅ Všechny změny ověřeny  
**Backup:** ✅ Git commit před změnami doporučen
