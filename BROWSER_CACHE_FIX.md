# Oprava CSS - Browser Cache Problem

## ✅ Diagnostika kompletní

### Co funguje:
1. ✅ **Nginx servíruje CSS správně**
   ```bash
   curl -I https://aristocraticartworksale.com/css/site-new.css
   # HTTP/2 200
   # content-type: text/css
   ```

2. ✅ **HTML obsahuje správný `<link>` tag**
   ```html
   <link rel="stylesheet" href="/css/site-new.css?v=22" />
   ```

3. ✅ **Static files jsou v shared volume**
   - `/app/wwwroot/css/site-new.css` existuje
   - Velikost: 11438 bytes

### Problém:
❌ **Prohlížeč má cachovanou STAROU verzi stránky BEZ CSS**

## Řešení / Solution

### Pro uživatele (testování webu):

#### Metoda 1: Hard Refresh (NEJRYCHLEJŠÍ)
```
Windows/Linux:  Ctrl + Shift + R  nebo  Ctrl + F5
Mac:            Cmd + Shift + R
```

#### Metoda 2: Vymazat cache v DevTools
1. Otevřít stránku: `https://aristocraticartworksale.com`
2. Stisknout `F12` (DevTools)
3. **Pravé tlačítko** na refresh button (vedle URL)
4. Vybrat: **"Empty Cache and Hard Reload"**

#### Metoda 3: Vymazat všechny cookies & cache
**Chrome:**
1. `F12` → Application tab
2. Clear storage → Clear site data

**Firefox:**
1. `F12` → Storage tab  
2. Right click → Clear All

**Edge:**
1. `F12` → Application tab
2. Storage → Clear storage

### Ověření, že CSS se načítá:

1. **Otevřít DevTools (`F12`)**
2. **Network tab**
3. **Hard refresh (`Ctrl+Shift+R`)**
4. **Zkontrolovat:**
   ```
   Name                    Status    Type         Size
   ─────────────────────────────────────────────────────
   site-new.css?v=22       200       text/css     11.2 KB
   bootstrap.min.css       200       text/css     ~200 KB
   site.js                 200       application/javascript
   ```

5. **Všechny CSS soubory musí mít status 200!**

### Co by jste měli vidět po načtení CSS:

✅ **Homepage:**
- Logo normální velikost (ne obří)
- Zlatá/tmavá barevná schémata
- Centrovaný text
- Stylizované tlačítka

✅ **Collections:**
- Grid layout kolekcí
- Správné karty s obrázky
- Hover efekty

✅ **Contact:**
- Stylizovaný formulář
- Input pole s correct width
- Správné spacing

✅ **Login:**
- Centrovaný formulář
- Stylizované input fieldy
- Správné fonty (Playfair Display + Inter)

## Pro vývojáře: Změnit cache strategii

Pokud problém přetrvává, změňte cache headers v Nginx:

### Současná konfigurace (agresivní cache):
```nginx
location /css/ {
    alias /app/wwwroot/css/;
    expires 1y;  # ← Cache 1 rok
    add_header Cache-Control "public, immutable";
}
```

### Navrhovaná změna (během vývoje):
```nginx
location /css/ {
    alias /app/wwwroot/css/;
    expires -1;  # ← Žádná cache
    add_header Cache-Control "no-cache, no-store, must-revalidate";
    add_header Pragma "no-cache";
}
```

Nebo používat version query string (už implementováno):
```html
<link rel="stylesheet" href="/css/site-new.css?v=23" />
                                                   ↑
                                    Zvyšte číslo při změnách
```

## Testovací příkazy

```bash
# Test 1: Zkontrolovat, že CSS se servíruje
curl -I https://aristocraticartworksale.com/css/site-new.css

# Mělo by vrátit:
# HTTP/2 200
# content-type: text/css

# Test 2: Stáhnout a zkontrolovat obsah
curl -s https://aristocraticartworksale.com/css/site-new.css | head -20

# Mělo by zobrazit CSS kód, např:
# /* Custom styles */
# :root { ... }

# Test 3: Zkontrolovat HTML
curl -s https://aristocraticartworksale.com/ | grep "css/site-new.css"

# Mělo by vrátit:
# <link rel="stylesheet" href="/css/site-new.css?v=22" />
```

## Troubleshooting

### CSS se stále nenačítá po hard refresh:

1. **Zkontrolovat browser console (F12):**
   ```
   Hledat errory typu:
   - "Failed to load resource"
   - "CSP violation"
   - "net::ERR_"
   ```

2. **Test v Incognito/Private mode:**
   ```
   Chrome: Ctrl+Shift+N
   Firefox: Ctrl+Shift+P
   Edge: Ctrl+Shift+N
   ```
   Pokud funguje v incognito → problém je cache

3. **Test v jiném prohlížeči:**
   - Chrome
   - Firefox
   - Edge
   - Safari

4. **Zkontrolovat Nginx error log:**
   ```bash
   cd /AAS
   docker logs aas-nginx-prod --tail 100 | grep -i error
   ```

5. **Zkontrolovat, že soubory jsou v shared volume:**
   ```bash
   docker exec aas-nginx-prod ls -la /app/wwwroot/css/
   ```

### Stále nefunguje?

Zavolejte diagnostický skript:
```bash
cd /AAS
cat > test-css-complete.sh << 'EOF'
#!/bin/bash
echo "=== CSS Diagnostic ==="
echo ""
echo "1. File exists in source?"
ls -lh src/AAS.Web/wwwroot/css/site-new.css
echo ""
echo "2. File in Nginx container?"
docker exec aas-nginx-prod ls -lh /app/wwwroot/css/site-new.css
echo ""
echo "3. External access test:"
curl -I https://aristocraticartworksale.com/css/site-new.css 2>&1 | head -10
echo ""
echo "4. HTML contains link?"
curl -s https://aristocraticartworksale.com/ | grep "site-new.css"
echo ""
echo "5. Nginx access log (CSS requests):"
docker logs aas-nginx-prod 2>&1 | grep "\.css" | tail -5
EOF
chmod +x test-css-complete.sh
./test-css-complete.sh
```

## Závěr / Summary

**CSS soubory se servírují správně ze serveru.**

Problém je na straně klienta (browser cache). Uživatelé musí provést:
1. **Hard refresh** (`Ctrl+Shift+R`)
2. Nebo otevřít v **Incognito mode**
3. Nebo vymazat **browser cache**

Po těchto krocích by CSS mělo fungovat perfektně! 🎨✨

---

**Všechny testy prošly ✅ - server je v pořádku!**
