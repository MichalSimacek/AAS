# Aristocratic Artwork Sale - Deployment Guide

## 🔒 BEZPEČNOST PŘEDEVŠÍM!

**VAROVÁNÍ:** Před nasazením si přečtěte [SECURITY.md](SECURITY.md) pro detailní bezpečnostní informace.

**KRITICKÉ:** NIKDY nepoužívejte výchozí hesla! Vždy nastavte silná, unikátní hesla pro:
- Databázový účet (`DB_PASSWORD`)
- Admin účet (`ADMIN_PASSWORD`)
- SMTP účet (`SMTP_PASSWORD`)

---

## 📋 Přehled

Tento návod pokrývá dva způsoby nasazení aplikace na Ubuntu server:
1. **Metoda A: Docker** (doporučeno pro rychlé nasazení)
2. **Metoda B: Bez Dockeru** (systemd + nginx)

---

## ⚙️ Požadavky

- **Ubuntu Server** 22.04 LTS nebo novější
- **Doména** nastavenou na IP adresu serveru (aristocraticartworksale.com)
- **Root/sudo přístup** k serveru
- **2GB+ RAM** a **10GB+ disk space**
- **Silná hesla** připravená pro databázi, admin účet a SMTP

---

## 🐳 Metoda A: Nasazení s Dockerem (DOPORUČENO)

### Krok 1: Příprava serveru

```bash
# Připojte se k serveru
ssh root@your-server-ip

# Aktualizujte systém
apt update && apt upgrade -y

# Nainstalujte Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
rm get-docker.sh

# Nainstalujte Docker Compose
apt install -y docker-compose-plugin

# Nainstalujte Git
apt install -y git
```

### Krok 2: Nahrání aplikace na server

**Možnost A - Přes Git (doporučeno):**
```bash
cd /var/www
git clone https://github.com/your-repo/aas.git
cd aas
```

**Možnost B - Přes SCP (z vašeho počítače):**
```bash
# Z vašeho lokálního počítače
cd C:\AAS
scp -r . root@your-server-ip:/var/www/aas
```

### Krok 3: Konfigurace (KRITICKÉ!)

```bash
cd /var/www/aas

# Vytvořte .env soubor z příkladu
cp .env.example .env

# DŮLEŽITÉ: Upravte .env soubor s vašimi vlastními hodnotami
nano .env
```

**KRITICKÉ: Nastavte všechny tyto hodnoty se silnými hesly:**

```bash
# Database Configuration
DB_PASSWORD=YOUR_STRONG_DB_PASSWORD_HERE   # Minimálně 16 znaků!

# Admin Account
ADMIN_EMAIL=admin@aristocraticartworksale.com
ADMIN_PASSWORD=YOUR_STRONG_ADMIN_PASSWORD_HERE   # Minimálně 12 znaků, velká/malá písmena, čísla, speciální znaky!

# SMTP Configuration
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USE_STARTTLS=true
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-specific-password   # Pro Gmail použijte App Password!
EMAIL_FROM=no-reply@aristocraticartworksale.com
EMAIL_TO=aristocratic-artwork-sell@proton.me

# Translation (volitelné)
TRANSLATION_ENABLED=false
TRANSLATION_ENDPOINT=https://libretranslate.com/translate
TRANSLATION_API_KEY=
```

**🔐 Generování silných hesel:**
```bash
# Generovat náhodné silné heslo
openssl rand -base64 32

# Nebo použít pwgen
apt install pwgen
pwgen -s 32 1
```

### Krok 4: Spuštění s Dockerem

```bash
cd /var/www/aas

# Spusťte aplikaci
docker compose up -d

# Zkontrolujte, že běží
docker compose ps
docker compose logs -f web
```

Aplikace poběží na **http://your-server-ip:5000**

### Krok 5: Nastavení Nginx jako reverse proxy

```bash
# Nainstalujte Nginx
apt install -y nginx certbot python3-certbot-nginx

# Vytvořte konfiguraci
cat > /etc/nginx/sites-available/aas << 'EOF'
server {
    listen 80;
    server_name aristocraticartworksale.com www.aristocraticartworksale.com;

    client_max_body_size 100M;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
EOF

# Aktivujte konfiguraci
ln -sf /etc/nginx/sites-available/aas /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx
```

### Krok 6: SSL certifikát (HTTPS)

```bash
# Získejte SSL certifikát od Let's Encrypt
certbot --nginx -d aristocraticartworksale.com -d www.aristocraticartworksale.com

# Certbot automaticky upraví nginx konfiguraci
# Certifikát se automaticky obnovuje
```

### Hotovo! 🎉

Vaše aplikace běží na **https://aristocraticartworksale.com**

**Admin přihlášení:**
- URL: https://aristocraticartworksale.com/Identity/Account/Login
- Email: `admin@aristocraticartworksale.com`
- Heslo: `ChangeMe_Aristo#2025`

**Přístup k admin panelu:**
- URL: https://aristocraticartworksale.com/Admin/Collections

---

## 🔧 Metoda B: Nasazení bez Dockeru

### Krok 1: Instalace závislostí

```bash
ssh root@your-server-ip
cd /var/www
# Nahrajte aplikaci (git nebo scp)

# Spusťte instalační skript
cd /var/www/aas/deployment
chmod +x install.sh
./install.sh
```

### Krok 2: Konfigurace

```bash
cd /var/www/aas
nano src/AAS.Web/appsettings.json
# Upravte SMTP nastavení (stejně jako výše)
```

### Krok 3: Build a nasazení

```bash
cd /var/www/aas/deployment
chmod +x deploy.sh
./deploy.sh
```

### Krok 4: SSL certifikát

```bash
apt install -y certbot python3-certbot-nginx
certbot --nginx -d aristocraticartworksale.com -d www.aristocraticartworksale.com
```

### Hotovo! 🎉

---

## 📝 Základní příkazy pro správu

### Docker metoda:

```bash
# Zobrazit logy
docker compose logs -f web

# Restartovat aplikaci
docker compose restart web

# Zastavit aplikaci
docker compose down

# Aktualizovat aplikaci
git pull  # nebo nahrajte nové soubory
docker compose up -d --build

# Zálohovat databázi
docker exec aas_postgres pg_dump -U aas aas > backup.sql
```

### Bez Docker metody:

```bash
# Zobrazit status
systemctl status aas

# Zobrazit logy
journalctl -u aas -f

# Restartovat aplikaci
systemctl restart aas

# Aktualizovat aplikaci
cd /var/www/aas
git pull  # nebo nahrajte nové soubory
cd deployment
./update.sh

# Zálohovat databázi
sudo -u postgres pg_dump aas > backup.sql
```

---

## 🔐 Bezpečnostní doporučení

1. **Změňte heslo do databáze** v `appsettings.json` a `docker-compose.yml`
2. **Změňte admin heslo** po prvním přihlášení
3. **Nastavte firewall:**
```bash
ufw allow 22/tcp   # SSH
ufw allow 80/tcp   # HTTP
ufw allow 443/tcp  # HTTPS
ufw enable
```
4. **Pravidelně aktualizujte systém:**
```bash
apt update && apt upgrade -y
docker compose pull  # pokud používáte Docker
```

---

## 📧 Konfigurace emailu

### Gmail (doporučeno pro testování):

1. Povolte 2FA v Google účtu
2. Vygenerujte App Password: https://myaccount.google.com/apppasswords
3. V `appsettings.json`:
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseStartTls": true,
    "Username": "your-email@gmail.com",
    "Password": "your-16-char-app-password"
  }
}
```

### ProtonMail Bridge (pro produkci):

1. Nainstalujte ProtonMail Bridge
2. Použijte SMTP údaje z Bridge

---

## 🌍 Nastavení překladu

**Možnost A - LibreTranslate.com (zdarma, veřejné API):**
```json
{
  "Translation": {
    "Endpoint": "https://libretranslate.com/translate",
    "ApiKey": "",
    "Enabled": true
  }
}
```

**Možnost B - Vlastní LibreTranslate instance:**
```bash
docker run -d -p 5001:5000 libretranslate/libretranslate
```
```json
{
  "Translation": {
    "Endpoint": "http://localhost:5001/translate",
    "Enabled": true
  }
}
```

**Možnost C - Vypnout automatický překlad:**
```json
{
  "Translation": {
    "Enabled": false
  }
}
```

---

## 🐛 Řešení problémů

### Aplikace se nespustí

```bash
# Docker
docker compose logs web

# Bez Docker
journalctl -u aas -n 100
```

### Databáze není dostupná

```bash
# Docker
docker compose ps
docker compose logs postgres

# Bez Docker
systemctl status postgresql
sudo -u postgres psql -l
```

### 502 Bad Gateway

```bash
# Zkontrolujte, že aplikace běží
curl http://localhost:5000

# Restartujte nginx
systemctl restart nginx
```

### Obrázky se nenahrávají

```bash
# Zkontrolujte oprávnění
chown -R www-data:www-data /var/www/aas/uploads  # nebo
docker exec aas_web ls -la /app/wwwroot/uploads
```

---

## 📊 Monitoring

```bash
# Využití disku
df -h

# Velikost databáze
sudo -u postgres psql -c "SELECT pg_size_pretty(pg_database_size('aas'));"

# Docker využití
docker stats
```

---

## 🎯 Checklist po nasazení

- [ ] Aplikace běží a je dostupná přes doménu
- [ ] HTTPS certifikát funguje
- [ ] Email odesílání funguje (test přes formulář "I'm interested")
- [ ] Překlad mezi jazyky funguje
- [ ] Admin login funguje
- [ ] Nahrávání obrázků funguje
- [ ] Nahrávání audio souborů funguje
- [ ] Admin heslo změněno
- [ ] Databázové heslo změněno
- [ ] Firewall nastaven
- [ ] Zálohování databáze nastaveno

---

## 📞 Kontakt a podpora

- **Aplikace:** https://aristocraticartworksale.com
- **Admin panel:** https://aristocraticartworksale.com/Admin/Collections
- **Email kontakt:** aristocratic-artwork-sell@proton.me

---

**Poznámka:** Tento web automaticky vytváří admin účet při prvním spuštění s přihlašovacími údaji uvedenými v `appsettings.json`.
