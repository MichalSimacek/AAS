# 🔐 Plán záloh AAS aplikace

## 📋 Co se zálohuje

1. **PostgreSQL databáze** (database.sql.gz)
   - Všechny kolekce, inquiries, komentáře, uživatelé
   - Komprimované gzip pro úsporu místa

2. **Nahrané soubory** (uploads.tar.gz)
   - Všechny obrázky kolekcí
   - Různé velikosti (480px, 1600px, originál)

3. **Konfigurace** (config.tar.gz)
   - docker-compose.prod.yml
   - .env soubory

4. **Metadata** (backup_info.txt)
   - Datum zálohy
   - Stav Docker kontejnerů
   - Velikost databáze
   - Využití disku

---

## 🚀 Instalace na serveru

### Krok 1: Příprava
```bash
# Připoj se na server
ssh root@YOUR_SERVER_IP

# Přejdi do adresáře projektu
cd /AAS
```

### Krok 2: Nahrání skriptů
```bash
# Zkopíruj tyto soubory na server:
# - backup-setup.sh
# - setup-remote-sync.sh
# - BACKUP_INSTRUCTIONS.md

# Nebo použij git pull pokud jsou v repozitáři
```

### Krok 3: Spuštění základního setupu
```bash
# Udělej skripty spustitelné
chmod +x backup-setup.sh setup-remote-sync.sh

# Spusť základní setup
./backup-setup.sh
```

### Krok 4: Získání hesla k zálohovacímu serveru
```bash
# 1. Otevři v prohlížeči:
# https://admin.masterdc.com/sharing/showpass?id=2908&hash=9081592-4602710001763-3161599

# 2. Zkopíruj heslo
```

### Krok 5: Nastavení vzdálené synchronizace
```bash
# Spusť remote sync setup (zadáš heslo)
./setup-remote-sync.sh
```

### Krok 6: Test zálohy
```bash
# Spusť první zálohu ručně
/AAS/backup.sh

# Zkontroluj výstup
ls -lh /AAS/local-backups/

# Zkontroluj remote sync
source /root/.backup_credentials && /AAS/sync-to-remote.sh
```

---

## ⏰ Harmonogram záloh

### Automatické zálohy
- **Frekvence**: Denně v 2:00 ráno
- **Cron job**: `0 2 * * * /AAS/backup.sh`
- **Retention**: 7 dní lokálně

### Co se děje při záloze
1. Vytvoří se snapshot PostgreSQL databáze
2. Zazipují se nahrané soubory
3. Zazálohuje se konfigurace
4. Vytvoří se info soubor
5. Synchronizace na remote FTP server
6. Vyčistí se zálohy starší než 7 dní

---

## 📦 Struktura zálohy

```
/AAS/local-backups/
└── 20250120_020000/
    ├── database.sql.gz         # PostgreSQL dump (komprimovaný)
    ├── uploads.tar.gz          # Všechny obrázky
    ├── config.tar.gz           # Konfigurace
    └── backup_info.txt         # Metadata
```

---

## 🔄 Obnovení ze zálohy

### 1. Obnovení databáze
```bash
# Rozbal dump
cd /AAS/local-backups/20250120_020000/
gunzip database.sql.gz

# Obnov do PostgreSQL
docker exec -i aas-db-prod psql -U postgres aas < database.sql
```

### 2. Obnovení souborů
```bash
# Rozbal uploads
cd /AAS
tar -xzf local-backups/20250120_020000/uploads.tar.gz

# Nastav správná oprávnění
chown -R 33:33 /AAS/uploads  # www-data user in container
```

### 3. Obnovení konfigurace
```bash
# Rozbal config
tar -xzf local-backups/20250120_020000/config.tar.gz

# Restart služeb
cd /AAS
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d
```

---

## 📊 Monitoring záloh

### Kontrola logů
```bash
# Zobraz poslední zálohy
tail -f /var/log/aas-backup.log

# Kontrola cron jobu
crontab -l
```

### Kontrola velikosti
```bash
# Lokální zálohy
du -sh /AAS/local-backups/*

# Vzdálený server (přes FTP)
ftp backup15.master.cz
# user: bcp-id-9316
# cat user_quota
```

### Test integrity
```bash
# Test PostgreSQL dump
gunzip -c database.sql.gz | head -100

# Test tar archivu
tar -tzf uploads.tar.gz | head -20
```

---

## 🔔 Monitoring serveru (NRPE)

### Co ti poslal hosting
- Konfigurační soubor: `nrpe.cfg` → `/etc/nagios/nrpe.cfg`
- 3 pluginy → `/usr/lib64/nagios/plugins/`

### Instalace NRPE monitoringu
```bash
# 1. Nainstaluj NRPE
apt update
apt install -y nagios-nrpe-server nagios-plugins

# 2. Zkopíruj konfiguraci od hostingu
# (měl jsi ji dostat v příloze emailu)
cp nrpe.cfg /etc/nagios/nrpe.cfg

# 3. Zkopíruj pluginy
cp check_* /usr/lib64/nagios/plugins/
chmod +x /usr/lib64/nagios/plugins/check_*

# 4. Restart NRPE
systemctl restart nagios-nrpe-server
systemctl enable nagios-nrpe-server

# 5. Informuj hosting že je hotovo
# Oni pak přidají monitoring CPU, RAM, disk
```

---

## ⚠️ Důležité poznámky

1. **Heslo k zálohovacímu serveru**
   - Je uloženo v `/root/.backup_credentials`
   - Tento soubor má permissions 600 (jen root)
   - Pro produkci zvažte použití vault (HashiCorp Vault, AWS Secrets Manager)

2. **Kapacita zálohovacího prostoru**
   - Máte 100 GB
   - Aktuální využití: `cat user_quota` na FTP serveru
   - Sledujte pravidelně!

3. **Retention policy**
   - Lokálně: 7 dní
   - Remote: Neomezeno (dokud nezbyde místo)
   - Zvažte periodické čištění starých remote záloh

4. **Testování obnovy**
   - Měsíčně otestujte obnovu na testovacím serveru
   - Zajistěte, že zálohy jsou funkční

---

## 📞 Kontakt na podporu

**Master.cz hosting**
- Email: Viz vaše původní komunikace
- Web: https://www.master.cz/
- Dokumentace: https://www.master.cz/help/

---

## ✅ Checklist

- [ ] Spuštěn `backup-setup.sh`
- [ ] Získáno heslo k zálohovacímu serveru
- [ ] Spuštěn `setup-remote-sync.sh`
- [ ] První záloha úspěšná (`/AAS/backup.sh`)
- [ ] Remote sync funguje
- [ ] Cron job nastaven (viditelný v `crontab -l`)
- [ ] Nainstalován NRPE monitoring
- [ ] Informován hosting o dokončení NRPE
- [ ] Otestována obnova databáze
- [ ] Nastaveny alerty pro plnou kapacitu
