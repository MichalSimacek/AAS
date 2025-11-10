# 🚀 Stručný návod - Spuštění aplikace v produkci

## ✅ Prerekvizity (zkontroluj nejdříve)

```bash
# 1. Docker běží?
docker --version
docker ps

# 2. Máš .env.production?
ls -la .env.production

# 3. Jsou porty 80 a 443 volné?
sudo netstat -tulpn | grep -E ':80|:443'
```

---

## 🎯 Rychlé spuštění (3 kroky)

### Krok 1: Nastav environment proměnné
Zkontroluj/vytvoř `.env.production` soubor:

```bash
# Edituj .env.production
nano .env.production
```

**Minimální požadované proměnné:**
```env
# Database
DB_HOST=db
DB_PORT=5432
DB_NAME=aas_production
DB_USER=aasuser
DB_PASSWORD=tvoje_silne_heslo

# Admin
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=admin_silne_heslo

# Email (pokud máš)
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=tvuj@email.com
EMAIL_PASSWORD=email_heslo
EMAIL_FROM=noreply@yourdomain.com
```

### Krok 2: Spusť deployment script
```bash
# Dej oprávnění
chmod +x deploy.sh

# Spusť
./deploy.sh
```

Script automaticky:
- ✅ Zkontroluje prerekvizity
- ✅ Vytvoří SSL certifikáty (Let's Encrypt)
- ✅ Sestaví Docker image
- ✅ Spustí všechny služby
- ✅ Ověří funkčnost

### Krok 3: Zkontroluj, že vše běží
```bash
# Zkontroluj kontejnery
docker ps

# Zkontroluj logy
docker-compose -f docker-compose.prod.yml logs -f

# Test statických souborů
./test-static-files.sh aristocraticartworksale.com
```

---

## 🔧 Alternativa: Manuální spuštění

Pokud deploy.sh nefunguje nebo chceš více kontroly:

```bash
# 1. Zastavit staré kontejnery
docker-compose -f docker-compose.prod.yml down

# 2. Build s čistým cache
docker-compose -f docker-compose.prod.yml build --no-cache

# 3. Spustit služby
docker-compose -f docker-compose.prod.yml up -d

# 4. Sledovat logy
docker-compose -f docker-compose.prod.yml logs -f
```

---

## 🧪 Ověření funkčnosti

### 1. Zkontroluj běžící kontejnery
```bash
docker ps
```

Měl bys vidět:
- ✅ `aas-web-prod` (nebo podobný název)
- ✅ `aas-nginx-prod`
- ✅ `aas-db-prod`

### 2. Test statických souborů
```bash
# Pomocí test scriptu
./test-static-files.sh aristocraticartworksale.com

# Nebo manuálně
curl -I https://aristocraticartworksale.com/css/site.css
curl -I https://aristocraticartworksale.com/js/site.js
```

**Očekávaný výsledek:** `HTTP/2 200 OK` ✅

### 3. Test v prohlížeči
- Otevři: `https://aristocraticartworksale.com`
- Zkontroluj Dev Console (F12) - žádné 400 chyby
- Stránka vypadá správně (CSS se načetl)

---

## 📊 Monitoring (prvních 30 minut)

```bash
# Sleduj logy real-time
docker-compose -f docker-compose.prod.yml logs -f

# Nebo specifický kontejner
docker logs -f aas-web-prod
docker logs -f aas-nginx-prod

# Zkontroluj Nginx error log
docker exec aas-nginx-prod tail -f /var/log/nginx/error.log
```

---

## ⚠️ Řešení problémů

### Problém: Deploy.sh selže
```bash
# Zkontroluj logy
./deploy.sh 2>&1 | tee deploy.log

# Nebo použij manuální cestu výše
```

### Problém: Kontejnery se nespustí
```bash
# Zkontroluj logy
docker-compose -f docker-compose.prod.yml logs

# Zkontroluj konfiguraci
docker-compose -f docker-compose.prod.yml config

# Restartuj Docker
sudo systemctl restart docker
```

### Problém: Statické soubory 404
```bash
# Zkontroluj, že jsou v Nginx
docker exec aas-nginx-prod ls -la /app/wwwroot/

# Restart web kontejneru (zkopíruje znovu)
docker-compose -f docker-compose.prod.yml restart web
sleep 10
docker-compose -f docker-compose.prod.yml restart nginx
```

### Problém: Database connection error
```bash
# Zkontroluj, že DB běží
docker ps | grep db

# Zkontroluj DB logy
docker logs aas-db-prod

# Zkontroluj připojení
docker exec aas-web-prod ping db
```

---

## 📝 Užitečné příkazy

```bash
# Restart všeho
docker-compose -f docker-compose.prod.yml restart

# Restart pouze web
docker-compose -f docker-compose.prod.yml restart web

# Restart pouze nginx
docker-compose -f docker-compose.prod.yml restart nginx

# Zastavit vše
docker-compose -f docker-compose.prod.yml down

# Zastavit a smazat volumes
docker-compose -f docker-compose.prod.yml down -v

# Rebuild a restart
docker-compose -f docker-compose.prod.yml up -d --build --force-recreate

# Sledovat logy
docker-compose -f docker-compose.prod.yml logs -f

# Interaktivní shell
docker exec -it aas-web-prod bash
docker exec -it aas-nginx-prod sh
```

---

## 🔄 Update aplikace

```bash
# 1. Git pull
git pull

# 2. Rebuild
docker-compose -f docker-compose.prod.yml build --no-cache

# 3. Restart
docker-compose -f docker-compose.prod.yml up -d --force-recreate

# 4. Zkontroluj
docker-compose -f docker-compose.prod.yml ps
```

---

## ✅ Checklist úspěšného nasazení

Po nasazení zkontroluj:

- [ ] Všechny kontejnery běží (`docker ps`)
- [ ] Web je dostupný na HTTPS
- [ ] CSS/JS se načítají (200 status)
- [ ] Žádné chyby v Nginx error log
- [ ] Admin login funguje
- [ ] Database connection funguje
- [ ] Obrázky se načítají
- [ ] Formuláře fungují

---

## 🎯 Cílový stav

**Když vše funguje správně, uvidíš:**

1. **Docker PS:**
   ```
   CONTAINER ID   IMAGE              STATUS         PORTS                    NAMES
   xxx            aas-web-prod      Up 5 minutes   0.0.0.0:5000->5000/tcp   aas-web-prod
   xxx            nginx:alpine      Up 5 minutes   0.0.0.0:80->80/tcp       aas-nginx-prod
   xxx            postgres:15       Up 5 minutes   5432/tcp                 aas-db-prod
   ```

2. **Curl test:**
   ```bash
   $ curl -I https://yourdomain.com/css/site.css
   HTTP/2 200
   content-type: text/css
   cache-control: public, immutable
   ```

3. **Prohlížeč:**
   - ✅ Stránka vypadá správně
   - ✅ Dev Console bez chyb
   - ✅ Rychlé načítání

---

## 📞 Potřebuješ pomoct?

1. **Přečti troubleshooting výše** ⬆️
2. **Zkontroluj logy** 📋
3. **Spusť test script** 🧪
4. **Kontaktuj podporu s logy** 📞

---

## 🎉 Gratulujeme!

Pokud vše funguje, máš úspěšně nasazenou aplikaci s opravou HTTP 400! 🚀

**Důležité:**
- Sleduj logy prvních 24 hodin
- Zálohuj databázi pravidelně
- Udržuj Docker aktualizovaný

**Další informace:**
- [README_HTTP_400_FIX.md](README_HTTP_400_FIX.md) - Detaily o opravě
- [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) - Kompletní checklist
