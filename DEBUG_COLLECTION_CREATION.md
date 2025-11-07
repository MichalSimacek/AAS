# Debug Guide - Collection Creation

## ✅ Implementované opravy

### 1. Success Message (TempData)
Po úspěšném vytvoření kolekce se zobrazí zelený alert:
```
Success! Collection 'Your Title' created successfully!
```

### 2. Error Display
Všechny chyby z ModelState se nyní zobrazují v červeném alertu nahoře formuláře.

### 3. Collection Count
Admin Index nyní zobrazuje: "Total: X collection(s)"

### 4. Multiple Images Upload
Input má atribut `multiple`, takže lze vybrat více obrázků najednou:
```html
<input type="file" name="images" multiple accept="image/*" class="form-control" />
```

## 🔍 Debug Checklist

Pokud kolekce není vidět po vytvoření:

### Krok 1: Zkontrolovat Success Message
- [ ] Zobrazil se zelený alert "Success!" na Admin Index?
- [ ] Pokud ANO → kolekce byla vytvořena, ale může být prázdná
- [ ] Pokud NE → došlo k chybě, zkontrolujte errory

### Krok 2: Zkontrolovat Error Messages
Pokud se vrátíte na Create form s červeným alertem, přečtěte si chyby:

**Možné chyby:**
- "At least one image is required" → nebyly vybrány žádné obrázky
- "File type .xxx is not allowed" → špatný formát obrázku
- "File size exceeds maximum" → obrázek je příliš velký (>10MB)
- "Error uploading images: ..." → problém při uploadu

### Krok 3: Zkontrolovat Upload Directory
```bash
# Zkontrolovat, že adresáře existují
ls -la /app/src/AAS.Web/wwwroot/uploads/images/

# Zkontrolovat oprávnění
chmod -R 755 /app/src/AAS.Web/wwwroot/uploads/
```

### Krok 4: Zkontrolovat Database
```sql
-- Zkontrolovat, že kolekce byla vytvořena
SELECT * FROM "Collections" ORDER BY "CreatedUtc" DESC LIMIT 5;

-- Zkontrolovat obrázky
SELECT ci.*, c."Title" 
FROM "CollectionImages" ci 
JOIN "Collections" c ON ci."CollectionId" = c."Id"
ORDER BY c."CreatedUtc" DESC;
```

### Krok 5: Zkontrolovat Application Logs
```bash
# Docker logs
docker logs <container-name> --tail 100

# Hledejte:
# - "Collection 'XXX' created successfully"
# - Transaction rollback errors
# - ImageService errors
```

## 📸 Upload Multiple Images - Návod

### Způsob 1: Ctrl+Click
1. Klikněte na "Choose Files" u Images
2. Držte **Ctrl** (nebo **Cmd** na Mac)
3. Klikejte na jednotlivé obrázky
4. Klikněte "Open"

### Způsob 2: Shift+Click
1. Klikněte na "Choose Files"
2. Klikněte na první obrázek
3. Držte **Shift**
4. Klikněte na poslední obrázek
5. Všechny obrázky mezi nimi se vyberou

### Způsob 3: Drag & Drop (pokud podporováno)
1. Vyberte více souborů v exploreru
2. Přetáhněte je na input pole

## 🐛 Známé problémy a řešení

### Problém: Kolekce se vytvoří, ale bez obrázků
**Příčina:** Transaction rollback kvůli chybě v ImageService

**Řešení:**
1. Zkontrolujte formát obrázků (JPG, PNG, WebP)
2. Zkontrolujte velikost (<10MB každý)
3. Zkontrolujte, že nejsou poškozené

**Debug:**
```csharp
// V ImageService.cs přidejte logging
Console.WriteLine($"Processing image: {file.FileName}, Size: {file.Length}");
```

### Problém: 404 po vytvoření
**Příčina:** Redirect na špatnou URL

**Aktuální fix:**
```csharp
return RedirectToAction(nameof(Index), new { area = "Admin" });
// Redirectuje na: /Admin/Collections
```

### Problém: Obrázky se nahrají, ale nejsou vidět na webu
**Příčina:** Varianty se nevytvořily správně

**Zkontrolujte:**
```bash
# Měly by existovat 4 verze každého obrázku:
ls -la /app/src/AAS.Web/wwwroot/uploads/images/

# Příklad pro obrázek s GUID "abc123":
# abc123.jpg      (originál)
# abc123-1600.jpg (large)
# abc123-960.jpg  (medium)
# abc123-480.jpg  (thumbnail)
```

### Problém: "Connection string is not configured"
**Příčina:** PostgreSQL není nakonfigurován

**Řešení:**
V `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aas_db;Username=postgres;Password=yourpassword"
  }
}
```

## 📋 Testovací Scenario

### Test 1: Jednoduchá kolekce
1. Title: "Test Collection"
2. Category: Paintings
3. Description: "Test description"
4. Images: 1 obrázek (JPG, <2MB)
5. Kliknout Create Collection
6. **Očekáváno:** Zelený alert, redirect na Index, kolekce viditelná

### Test 2: Více obrázků
1. Title: "Multi Image Collection"
2. Category: Jewelry
3. Description: "Multiple images test"
4. Images: 5 obrázků (JPG/PNG mix)
5. Kliknout Create Collection
6. **Očekáváno:** Všech 5 obrázků nahráno, každý má 3 varianty

### Test 3: S Audio
1. Title: "Audio Collection"
2. Category: Statues
3. Description: "With audio"
4. Images: 2 obrázky
5. Audio: 1 MP3 soubor (<5MB)
6. Kliknout Create Collection
7. **Očekáváno:** Kolekce s audio, přehrávač viditelný na detail stránce

## 🔧 Quick Fixes

### Rychlé zobrazení všech kolekcí (pro debugging)
V `Index.cshtml`, přidejte na začátek:
```html
<div class="alert alert-info">
    Debug: Found @Model.Count() collections
    @foreach(var c in Model)
    {
        <div>- @c.Title (@c.Images.Count images)</div>
    }
</div>
```

### Force reload bez cache
Přidejte do URL: `?nocache=1`

### Clear TempData (pokud je stuck)
V controlleru:
```csharp
TempData.Clear();
```

## 📞 Support

Pokud problémy přetrvávají:
1. Zkontrolujte všechny kroky výše
2. Podívejte se na browser console (F12) → Network tab
3. Zkontrolujte POST request k `/Admin/Collections/Create`
4. Podívejte se na Response (should be 302 redirect on success)

---

**Status:** Všechny opravy implementovány ✅
**Next:** Test vytvoření kolekce s více obrázky
