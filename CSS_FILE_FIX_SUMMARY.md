# Oprava CSS - Použití správného souboru

## Skutečný problém

❌ **`site-new.css` je NEÚPLNÝ soubor!**

```bash
site-new.css:  588 řádků  ❌ (neúplný)
site.css:     1326 řádků  ✅ (kompletní)
```

**Proto design vypadal špatně:**
- Chyběly styly pro logo (bylo obří)
- Chyběly layout styly
- Chyběly styly pro formuláře
- Mnoho dalších stylů chybělo

## Řešení

✅ **Změněn `/AAS/src/AAS.Web/Views/Shared/_Layout.cshtml`**

**Před:**
```html
<link rel="stylesheet" href="/css/site-new.css?v=22" />
```

**Po:**
```html
<link rel="stylesheet" href="/css/site.css?v=23" />
```

## Jak aplikovat na serveru

### Metoda 1: Automatický skript (DOPORUČENO)
```bash
cd /AAS
chmod +x fix-css-use-correct-file.sh
./fix-css-use-correct-file.sh
```

Skript provede:
1. ✅ Rebuild web containeru (aby se zkopíroval nový CSS)
2. ✅ Restart všech služeb
3. ✅ Ověření, že site.css se servíruje

### Metoda 2: Manuální postup
```bash
cd /AAS

# Rebuild web container
docker compose -f docker-compose.prod.yml build web

# Restart služeb
docker compose -f docker-compose.prod.yml --env-file .env.production down
docker compose -f docker-compose.prod.yml --env-file .env.production up -d

# Čekat na start
sleep 10

# Ověřit CSS
docker exec aas-nginx-prod ls -lh /app/wwwroot/css/site.css
curl -I https://aristocraticartworksale.com/css/site.css
```

## Ověření v prohlížeči

1. **Otevřít:** `https://aristocraticartworksale.com`
2. **Hard refresh:** `Ctrl+Shift+R` (Windows/Linux) nebo `Cmd+Shift+R` (Mac)
3. **F12 → Network tab**
4. **Zkontrolovat:**
   ```
   Name              Status    Type        Size
   ───────────────────────────────────────────────
   site.css?v=23     200       text/css    ~28 KB  ✅
   ```
   **NE** `site-new.css` ❌

## Co byste měli vidět po opravě:

### ✅ Homepage
- Logo **normální velikost** (ne obří)
- Hero sekce se správným **zlatým** barevným schématem
- Správné **spacing** a **layout**
- Stylizované tlačítka s hover efekty

### ✅ Collections
- **Grid layout** kolekcí
- Správné **karty** s obrázky
- **Hover efekty** na kartách
- Správné **fonty** (Playfair Display pro nadpisy)

### ✅ Contact
- **Centrovaný** formulář
- Input pole se **správnou šířkou**
- Stylizované tlačítko "Odeslat"
- Správné **spacing** mezi elementy

### ✅ Login/Register
- **Centrovaný** formulář
- Stylizované **input fieldy**
- Správné **fonty** (Inter pro text)
- **Zlaté** akcenty na tlačítkách

## Porovnání CSS souborů

### site-new.css (NEÚPLNÝ) ❌
```css
/* Pouze základní styly */
- 588 řádků
- Chybí mnoho komponent
- Nedostatečné responsive styly
- Chybí styly pro formuláře
```

### site.css (KOMPLETNÍ) ✅
```css
/* Kompletní styly */
- 1326 řádků
- Všechny komponenty
- Plné responsive styly
- Kompletní styly pro formuláře
- Všechny utility classes
- Všechny hover efekty
```

## Technické detaily

### Změněné soubory:
1. **`/AAS/src/AAS.Web/Views/Shared/_Layout.cshtml`**
   - Změněn link na CSS soubor
   - Zvýšena verze z `v=22` na `v=23` (cache busting)

### Docker build:
```bash
docker compose -f docker-compose.prod.yml build web
```
- Rebuilds ASP.NET Core aplikaci
- Kopíruje nový _Layout.cshtml
- Kopíruje site.css do výstupní složky

### Restart procesu:
1. Web container zkopíruje `/app/wwwroot/*` → `/shared-static/`
2. Nginx mountuje `/shared-static/` jako `/app/wwwroot/`
3. Browser requestuje `/css/site.css?v=23`
4. Nginx servíruje z `/app/wwwroot/css/site.css`

## Troubleshooting

### CSS se stále nenačítá správně:

**1. Zkontrolovat, že správný soubor se používá:**
```bash
curl -s https://aristocraticartworksale.com/ | grep "css/site"
# Mělo by vrátit:
# <link rel="stylesheet" href="/css/site.css?v=23" />
```

**2. Zkontrolovat velikost souboru:**
```bash
curl -I https://aristocraticartworksale.com/css/site.css | grep "content-length"
# Mělo by být: content-length: 27683  (nebo podobné číslo ~28KB)
```

**3. Zkontrolovat v shared volume:**
```bash
docker exec aas-nginx-prod ls -lh /app/wwwroot/css/
# Mělo by ukázat:
# -rw-r--r-- 1 root root  27K ... site.css
```

**4. Browser DevTools kontrola:**
```
F12 → Network → Filter: CSS
- Měli byste vidět: site.css?v=23 (Status 200, Size ~28KB)
- NE: site-new.css
```

### Stále neúplné styly:

**Zkontrolovat obsah souboru:**
```bash
docker exec aas-nginx-prod head -20 /app/wwwroot/css/site.css
```

Mělo by začínat podobně:
```css
/* ===================================
   ARISTOCRATIC ARTWORK SALE - CUSTOM STYLES
   ...
```

**Porovnat řádky:**
```bash
docker exec aas-nginx-prod wc -l /app/wwwroot/css/site.css
# Mělo by vrátit: 1326 /app/wwwroot/css/site.css
```

### Build selhal:

```bash
# Zkontrolovat build logs
docker compose -f docker-compose.prod.yml build web 2>&1 | tail -50

# Zkontrolovat, že source file existuje
ls -lh /AAS/src/AAS.Web/wwwroot/css/site.css
```

## Předchozí pokusy a co nefungovalo

❌ **Browser cache** - Nebyl to problém (CSS se načítal, ale byl neúplný)
❌ **Nginx konfigurace** - Fungovala správně
❌ **Docker volumes** - Fungovaly správně
✅ **Správný problém:** Špatný CSS soubor se používal (_Layout.cshtml)

## Závěr

**Root cause:** `_Layout.cshtml` odkazoval na `site-new.css`, který je neúplný.

**Solution:** Změněn odkaz na `site.css` (kompletní soubor).

**Expected result:** Kompletní a správné stylování celé aplikace.

---

**Po aplikování této opravy by měl web vypadat profesionálně a všechny styly by měly fungovat! 🎨✨**
