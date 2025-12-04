# Update Guide - Aplikace změn na běžící server

Tento návod popisuje, jak aplikovat všechny změny z development na váš produkční Ubuntu server.

---

## 📋 Přehled změn k aplikaci

### 1. ✅ Footer lokalizace (Issue #3)
- Footer nyní podporuje překlady podle jazyka

### 2. ✅ Account Settings redesign (Issue #6)
- Nový soft, zaoblený design
- Sidebar navigace vlevo
- Plynulé přechody

### 3. ✅ Navigační 404 fix (Issue #7)
- Opravena navigace z Account Settings

### 4. ✅ TinyMCE self-hosted (Issue #9)
- Editor plně funkční bez API key
- Žádné read-only omezení

---

## 🚀 Metoda 1: Git Pull + Rebuild (Doporučeno)

### Pro Docker Deployment

```bash
# 1. SSH do serveru
ssh user@your-server

# 2. Backup aktuálního stavu
cd /var/www/aristocratic-artwork-sale
sudo docker-compose -f docker-compose.prod.yml down
sudo tar -czf ../aas-backup-$(date +%Y%m%d).tar.gz .

# 3. Pull nové změny
git pull origin main

# 4. Rebuild a restart
sudo docker-compose -f docker-compose.prod.yml build --no-cache web
sudo docker-compose -f docker-compose.prod.yml up -d

# 5. Kontrola logů
sudo docker logs -f aas-web-prod
```

### Pro Systemd Deployment

```bash
# 1. SSH do serveru
ssh user@your-server

# 2. Backup
cd /var/www/aristocratic-artwork-sale
sudo systemctl stop aas-web
sudo tar -czf ../aas-backup-$(date +%Y%m%d).tar.gz /var/www/aas-app

# 3. Pull změny
git pull origin main

# 4. Rebuild
cd src/AAS.Web
dotnet publish -c Release -o /var/www/aas-app

# 5. Restart
sudo systemctl start aas-web
sudo systemctl status aas-web
```

---

## 🔧 Metoda 2: Manuální aplikace změn

Pokud nemůžete použít git pull, aplikujte změny ručně:

### Krok 1: Footer lokalizace

**Soubor:** `src/AAS.Web/Views/Shared/_Layout.cshtml`

Najděte v footeru (okolo řádku 121):
```html
<p class="mb-2"><strong>Aristocratic Artwork Sale</strong></p>
<p class="small mb-0">© @DateTime.UtcNow.Year All rights reserved. Discretion, Quality & Professionalism.</p>
```

Změňte na:
```html
<p class="mb-2"><strong>@L["Site Name"]</strong></p>
<p class="small mb-0">@string.Format(L["Footer rights text"], DateTime.UtcNow.Year)</p>
```

### Krok 2: Account Settings redesign

**A) Update _Layout.cshtml**

`src/AAS.Web/Areas/Identity/Pages/Account/Manage/_Layout.cshtml`

Nahraďte celý obsah souborem z development (viz attachment nebo git diff).

Klíčové změny:
- Sidebar navigace místo tab navigation
- JavaScript pro smooth transitions
- Nové hrefs pro nav items

**B) Update CSS**

`src/AAS.Web/wwwroot/css/site.css`

Přidejte nové styly pro Account Settings (okolo řádku 1365):
- `.account-settings-container`
- `.settings-sidebar`
- `.sidebar-nav`
- `.nav-item`
- `.settings-card`
- Responsive breakpoints

**C) Update View soubory**

Aktualizujte tyto soubory:
- `Areas/Identity/Pages/Account/Manage/Index.cshtml`
- `Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml`
- `Areas/Identity/Pages/Account/Manage/Email.cshtml`
- `Areas/Identity/Pages/Account/Manage/PersonalData.cshtml`

### Krok 3: TinyMCE Self-hosted

**A) Stáhněte TinyMCE**

```bash
cd /tmp
wget https://download.tiny.cloud/tinymce/community/tinymce_7.5.1.zip
unzip tinymce_7.5.1.zip
sudo cp -r tinymce/js/tinymce /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/lib/
```

**B) Update Create.cshtml**

`src/AAS.Web/Areas/Admin/Views/Blog/Create.cshtml`

Řádek 55, změňte:
```html
<script src="https://cdn.tiny.cloud/1/no-api-key/tinymce/6/tinymce.min.js" referrerpolicy="origin"></script>
```

Na:
```html
<script src="~/lib/tinymce/tinymce.min.js"></script>
```

A v konfiguraci přidejte:
```javascript
tinymce.init({
    // ... existing config
    promotion: false  // Přidat tento řádek
});
```

**C) Update Edit.cshtml**

Stejné změny v `src/AAS.Web/Areas/Admin/Views/Blog/Edit.cshtml` řádek 66.

### Krok 4: Program.cs - CSP update

`src/AAS.Web/Program.cs`

Řádek 182, přidejte `https://cdn.tiny.cloud`:
```csharp
"script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://code.jquery.com https://www.googletagmanager.com https://cdn.tiny.cloud; " +
```

**Poznámka:** Po nasazení self-hosted TinyMCE už není CSP pro cdn.tiny.cloud nutný, ale neškodí ho tam nechat.

---

## 📦 Automatický Update Script

Vytvořte script pro rychlou aktualizaci:

```bash
#!/bin/bash
# update-app.sh

echo "🔄 Updating Aristocratic Artwork Sale..."

# Backup
echo "📦 Creating backup..."
cd /var/www/aristocratic-artwork-sale
sudo docker-compose -f docker-compose.prod.yml down
sudo tar -czf ../aas-backup-$(date +%Y%m%d-%H%M%S).tar.gz .

# Pull changes
echo "⬇️  Pulling latest changes..."
git pull origin main

# Download TinyMCE if not exists
if [ ! -d "src/AAS.Web/wwwroot/lib/tinymce" ]; then
    echo "📥 Downloading TinyMCE..."
    cd /tmp
    wget -q https://download.tiny.cloud/tinymce/community/tinymce_7.5.1.zip
    unzip -q tinymce_7.5.1.zip
    mkdir -p /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/lib
    cp -r tinymce/js/tinymce /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/lib/
    rm -rf tinymce tinymce_7.5.1.zip
    cd /var/www/aristocratic-artwork-sale
fi

# Rebuild and restart
echo "🔨 Rebuilding application..."
sudo docker-compose -f docker-compose.prod.yml build --no-cache web

echo "🚀 Starting services..."
sudo docker-compose -f docker-compose.prod.yml up -d

echo "⏳ Waiting for startup (30s)..."
sleep 30

# Check status
echo "✅ Deployment status:"
sudo docker-compose -f docker-compose.prod.yml ps

echo ""
echo "📊 Application logs:"
sudo docker logs --tail 50 aas-web-prod

echo ""
echo "🎉 Update completed!"
echo ""
echo "🌐 Test at: https://your-domain.com"
echo "📝 View logs: sudo docker logs -f aas-web-prod"
```

Použití:
```bash
chmod +x update-app.sh
./update-app.sh
```

---

## ✅ Verifikace po nasazení

### 1. Zkontrolujte Footer
- Otevřete hlavní stránku
- Změňte jazyk (EN/CS/RU)
- ✅ Footer by se měl přeložit

### 2. Zkontrolujte Account Settings
- Přihlaste se jako user
- Jděte na Account Settings
- ✅ Měli byste vidět sidebar vlevo s 4 sekcemi
- ✅ Klikněte mezi sekcemi - mělo by být plynulé
- ✅ Design měl být zaoblený, soft

### 3. Zkontrolujte navigaci
- Z Account Settings klikněte na "Home" nebo jiný odkaz v menu
- ✅ Neměli byste dostat 404 error
- ✅ Mělo by vás to přesměrovat správně

### 4. Zkontrolujte TinyMCE
- Přihlaste se jako Admin
- Jděte na Admin → Blog → Create
- ✅ Editor by se měl načíst s plným toolbarem
- ✅ Měli byste umět psát a formátovat
- ✅ Žádné "read-only" varování v konzoli

### 5. Zkontrolujte konzoli prohlížeče
- Otevřete DevTools (F12)
- Přejděte na Console tab
- ✅ Žádné červené chyby
- ✅ Žádné CSP violations

---

## 🐛 Troubleshooting

### Aplikace nenaběhne po update

```bash
# Zkontrolujte logy
sudo docker logs aas-web-prod

# Nebo pro systemd
sudo journalctl -u aas-web -n 100

# Rollback na backup
sudo docker-compose -f docker-compose.prod.yml down
cd /var/www/
sudo tar -xzf aas-backup-TIMESTAMP.tar.gz -C aristocratic-artwork-sale/
cd aristocratic-artwork-sale
sudo docker-compose -f docker-compose.prod.yml up -d
```

### CSS změny se neprojeví

```bash
# Vyčistěte browser cache
Ctrl + Shift + R (hard refresh)

# Nebo vyčistěte server cache
sudo docker exec -it aas-web-prod bash
rm -rf /app/wwwroot/css/*.css
exit
sudo docker-compose restart web
```

### TinyMCE se nenačte

```bash
# Zkontrolujte, zda soubory existují
ls -la /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/lib/tinymce/

# Měli byste vidět:
# tinymce.min.js
# plugins/
# skins/
# themes/

# Pokud ne, stáhněte znovu
cd /tmp
wget https://download.tiny.cloud/tinymce/community/tinymce_7.5.1.zip
unzip tinymce_7.5.1.zip
sudo cp -r tinymce/js/tinymce /var/www/aristocratic-artwork-sale/src/AAS.Web/wwwroot/lib/
```

### 404 chyby přetrvávají

```bash
# Zkontrolujte JavaScript v _Layout.cshtml
# Ujistěte se, že hrefs jsou správné:
# /Identity/Account/Manage
# /Identity/Account/Manage/ChangePassword
# /Identity/Account/Manage/Email
# /Identity/Account/Manage/PersonalData

# Rebuild a restart
sudo docker-compose -f docker-compose.prod.yml restart
```

---

## 📝 Checklist před nasazením

- [ ] Backup aktuální verze vytvořen
- [ ] Git změny staženy nebo soubory ručně aktualizovány
- [ ] TinyMCE stažen do wwwroot/lib/
- [ ] Aplikace rebuild-nuta
- [ ] Služby restartovány
- [ ] Footer lokalizace funguje
- [ ] Account Settings design funguje
- [ ] Navigace bez 404 errorů
- [ ] TinyMCE editor funguje (ne read-only)
- [ ] Žádné console errory
- [ ] Všechny stránky dostupné

---

## 🔄 Rollback procedura

Pokud se něco pokazí:

```bash
# 1. Stop current version
sudo docker-compose -f docker-compose.prod.yml down

# 2. Restore from backup
cd /var/www/
sudo rm -rf aristocratic-artwork-sale
sudo tar -xzf aas-backup-TIMESTAMP.tar.gz
sudo mv aristocratic-artwork-sale-backup aristocratic-artwork-sale
cd aristocratic-artwork-sale

# 3. Start old version
sudo docker-compose -f docker-compose.prod.yml up -d

# 4. Verify
curl http://localhost:5000
```

---

## 📞 Potřebujete pomoc?

**Logy k diagnostice:**
```bash
# Docker
sudo docker logs -f aas-web-prod

# Systemd
sudo journalctl -u aas-web -f

# Nginx
sudo tail -f /var/log/nginx/error.log
```

**Kontrola stavu:**
```bash
# Docker
sudo docker-compose -f docker-compose.prod.yml ps

# Systemd
sudo systemctl status aas-web

# Ports
sudo netstat -tulpn | grep :5000
```

---

**Poznámka:** Tato aktualizace je **zpětně kompatibilní** - neměla by ovlivnit existující data nebo uživatele.
