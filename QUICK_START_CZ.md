# ⚡ RYCHLÝ START - 3 kroky k běžící aplikaci

## 📋 Před spuštěním (1 minuta)

```bash
# Zkontroluj Docker
docker --version && docker ps

# Zkontroluj .env.production
cat .env.production | grep -E "DB_PASSWORD|ADMIN_PASSWORD|ADMIN_EMAIL"
```

---

## 🚀 Spuštění (2 způsoby)

### A) Automaticky (doporučeno) ⭐
```bash
chmod +x deploy.sh
./deploy.sh
```
↳ Script vše udělá sám (5-10 minut)

### B) Manuálně (rychlejší)
```bash
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```
↳ Hotovo za 3-5 minut

---

## ✅ Ověření (30 sekund)

```bash
# 1. Běží kontejnery?
docker ps

# 2. Test statických souborů
./test-static-files.sh aristocraticartworksale.com

# 3. Otevři v prohlížeči
open https://aristocraticartworksale.com
```

---

## 📊 Sleduj logy

```bash
docker-compose -f docker-compose.prod.yml logs -f
```

Hledej:
- ✅ "Static files copied successfully"
- ✅ "Application started"
- ❌ Žádné "error" zprávy

---

## 🆘 Něco nefunguje?

### Statické soubory 400/404?
```bash
# Restart
docker-compose -f docker-compose.prod.yml restart web
sleep 10
docker-compose -f docker-compose.prod.yml restart nginx

# Zkontroluj
docker exec aas-nginx-prod ls -la /app/wwwroot/
```

### Kontejnery neběží?
```bash
# Logy
docker-compose -f docker-compose.prod.yml logs

# Rebuild
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d --force-recreate
```

### Jiný problém?
→ Přečti [NAVOD_SPUSTENI.md](NAVOD_SPUSTENI.md)

---

## 🎯 Očekávaný výsledek

Po úspěšném spuštění:

```bash
$ docker ps
aas-web-prod    Up 2 minutes
aas-nginx-prod  Up 2 minutes  
aas-db-prod     Up 2 minutes

$ curl -I https://yourdomain.com/css/site.css
HTTP/2 200 OK ✅
```

---

## 📚 Další dokumentace

| Problém | Dokument |
|---------|----------|
| HTTP 400 statické soubory | [HTTP_400_FIX_CARD.md](HTTP_400_FIX_CARD.md) |
| Detailní nasazení | [NAVOD_SPUSTENI.md](NAVOD_SPUSTENI.md) |
| Kompletní info | [README_HTTP_400_FIX.md](README_HTTP_400_FIX.md) |

---

**Tip:** První nasazení může trvat 5-10 minut kvůli build procesu. ⏱️
