# ✅ Deployment Checklist - HTTP 400 Fix

## 📋 Před nasazením

### 1. Backup
- [ ] Vytvořit Git commit aktuálního stavu
  ```bash
  git add .
  git commit -m "Backup before HTTP 400 fix"
  ```
- [ ] Zálohovat databázi (pokud je potřeba)
- [ ] Zaznamenat aktuální konfiguraci

### 2. Ověření prostředí
- [ ] Docker je nainstalován a běží
  ```bash
  docker --version
  docker-compose --version
  ```
- [ ] Máte root/sudo přístup
- [ ] Dostatečný diskový prostor (min 5GB volného)
  ```bash
  df -h
  ```

### 3. Příprava
- [ ] Všechny změny jsou commitnuty
- [ ] Žádné neuložené změny v souborech
- [ ] Environment proměnné jsou nastavené (.env soubor)

---

## 🚀 Nasazení

### Metoda A: Automatické nasazení (doporučeno)
- [ ] Spustit deployment script
  ```bash
  chmod +x QUICK_FIX_COMMANDS.sh
  ./QUICK_FIX_COMMANDS.sh
  ```
- [ ] Sledovat výstup a hledat chyby
- [ ] Počkat na dokončení (cca 5-10 minut)

### Metoda B: Manuální nasazení
- [ ] Zastavit běžící kontejnery
  ```bash
  docker-compose -f docker-compose.prod.yml down
  ```
- [ ] Rebuild kontejnerů
  ```bash
  docker-compose -f docker-compose.prod.yml build --no-cache
  ```
- [ ] Spustit kontejnery
  ```bash
  docker-compose -f docker-compose.prod.yml up -d
  ```
- [ ] Čekat 30-60 sekund na inicializaci

---

## 🔍 Verifikace

### 1. Kontrola kontejnerů
- [ ] Všechny kontejnery běží
  ```bash
  docker-compose -f docker-compose.prod.yml ps
  ```
  Očekávaný výstup:
  ```
  aas-web-prod    Up
  aas-nginx-prod  Up
  aas-db-prod     Up
  ```

### 2. Kontrola logů
- [ ] Web kontejner nemá chyby
  ```bash
  docker logs aas-web-prod --tail=50
  ```
  Hledat: "✅ Static files copied successfully"

- [ ] Nginx kontejner nemá chyby
  ```bash
  docker logs aas-nginx-prod --tail=50
  ```
  Žádné "error" zprávy

### 3. Kontrola statických souborů
- [ ] Soubory existují v Nginx kontejneru
  ```bash
  docker exec aas-nginx-prod ls -la /app/wwwroot/
  ```
  Očekáváno: css/, js/, images/ složky

- [ ] Soubory mají správná práva (readable)
  ```bash
  docker exec aas-nginx-prod ls -la /app/wwwroot/css/
  ```

### 4. HTTP testy
- [ ] CSS soubory vracejí 200
  ```bash
  curl -I https://yourdomain.com/css/site.css
  ```
  Očekáváno: `HTTP/2 200`

- [ ] JS soubory vracejí 200
  ```bash
  curl -I https://yourdomain.com/js/site.js
  ```
  Očekáváno: `HTTP/2 200`

- [ ] Obrázky vracejí 200
  ```bash
  curl -I https://yourdomain.com/images/logo.png
  ```
  Očekáváno: `HTTP/2 200`

### 5. Funkční test
- [ ] Otevřít web v prohlížeči
- [ ] Zkontrolovat, že CSS se načítá (stránka vypadá správně)
- [ ] Zkontrolovat Developer Console (F12) - žádné 400 chyby
- [ ] Otestovat několik stránek aplikace

### 6. Performance test
- [ ] Stránky se načítají rychle (cca 5-10ms pro statické soubory)
- [ ] Žádné timeouty
- [ ] Browser Network tab ukazuje cached soubory

---

## 🧪 Automatický test script

- [ ] Spustit test script
  ```bash
  chmod +x test-static-files.sh
  ./test-static-files.sh yourdomain.com
  ```
- [ ] Všechny testy projdou (zelené ✅)

---

## 📊 Monitoring (první hodina po nasazení)

### Každých 10 minut zkontrolovat:
- [ ] Nginx error log
  ```bash
  docker exec aas-nginx-prod tail -20 /var/log/nginx/error.log
  ```
  
- [ ] Web aplikace je dostupná
  ```bash
  curl -I https://yourdomain.com/
  ```

- [ ] Žádné chyby v Docker logs
  ```bash
  docker-compose -f docker-compose.prod.yml logs --tail=20
  ```

---

## ⚠️ Rollback plán (pokud něco selže)

### Rychlý rollback
1. [ ] Zastavit nové kontejnery
   ```bash
   docker-compose -f docker-compose.prod.yml down
   ```

2. [ ] Vrátit se na předchozí commit
   ```bash
   git reset --hard HEAD~1
   ```

3. [ ] Spustit staré kontejnery
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```

### Detailní rollback
- [ ] Restore Nginx konfigurace
  ```bash
  git checkout HEAD~1 -- nginx/nginx.conf nginx/nginx.prod.conf
  ```
  
- [ ] Restore Docker konfigurace
  ```bash
  git checkout HEAD~1 -- docker-compose.prod.yml Dockerfile.prod
  ```

- [ ] Rebuild a restart
  ```bash
  docker-compose -f docker-compose.prod.yml build
  docker-compose -f docker-compose.prod.yml up -d
  ```

---

## 📞 Troubleshooting

### Problém 1: Kontejnery se nespustí
- [ ] Zkontrolovat Docker logs
  ```bash
  docker-compose -f docker-compose.prod.yml logs
  ```
- [ ] Zkontrolovat dostupnost portů (80, 443, 5000)
  ```bash
  netstat -tulpn | grep -E ":80|:443|:5000"
  ```
- [ ] Zkontrolovat diskový prostor
  ```bash
  df -h
  ```

### Problém 2: Statické soubory nejsou v Nginx
- [ ] Zkontrolovat web kontejner logs
  ```bash
  docker logs aas-web-prod | grep "Static"
  ```
- [ ] Zkontrolovat volume
  ```bash
  docker volume ls | grep static
  docker volume inspect <volume_id>
  ```
- [ ] Manuálně zkopírovat soubory
  ```bash
  docker exec aas-web-prod /docker-entrypoint.sh
  ```

### Problém 3: Stále 400 chyba
- [ ] Ověřit Nginx konfiguraci
  ```bash
  docker exec aas-nginx-prod nginx -t
  ```
- [ ] Restart Nginx
  ```bash
  docker-compose -f docker-compose.prod.yml restart nginx
  ```
- [ ] Zkontrolovat Nginx error log detailně

### Problém 4: Něco jiného
- [ ] Přečíst STATIC_FILES_FIX.md
- [ ] Přečíst CHANGES_SUMMARY.md
- [ ] Kontaktovat podporu s logy

---

## ✅ Úspěšné nasazení potvrzeno když:

- [x] Všechny kontejnery běží (docker ps)
- [x] Statické soubory vracejí HTTP 200
- [x] Web je plně funkční
- [x] Žádné chyby v logách
- [x] Browser Dev Console bez chyb
- [x] Performance je dobrá nebo lepší než před nasazením
- [x] Test script prošel úspěšně

---

## 📝 Post-deployment poznámky

### Zaznamenat:
- [ ] Čas nasazení: _______________
- [ ] Verze před nasazením: _______________
- [ ] Verze po nasazení: _______________
- [ ] Jakékoliv problémy během nasazení: _______________
- [ ] Downtime (pokud byl): _______________

### Notifikace:
- [ ] Informovat tým o úspěšném nasazení
- [ ] Aktualizovat dokumentaci (pokud potřeba)
- [ ] Naplánovat monitoring na dalších 24 hodin

---

## 🎉 Gratulujeme!

Pokud jsou všechny checkboxy zaškrtnuté, nasazení bylo úspěšné!

HTTP 400 problém se statickými soubory je nyní vyřešen. 🚀

---

**Poznámky:**
- Tento checklist uložit pro budoucí nasazení
- V případě problémů použít rollback plán
- Kontaktovat podporu s logy pokud potřeba
