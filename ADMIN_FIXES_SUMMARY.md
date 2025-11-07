# Admin Panel Fixes - Summary

## ✅ Provedené opravy

### 1. Controller umístění a routing
- **Původní:** `/Controllers/Admin.cs` (špatné umístění)
- **Nové:** `/Areas/Admin/Controllers/CollectionsController.cs` (správné umístění)
- **Namespace:** `AAS.Web.Areas.Admin.Controllers`
- **Atributy:** `[Area("Admin")]` a `[Authorize(Roles = "Admin")]`

### 2. Opravené Tag Helpers ve Views

#### Admin/Index.cshtml
```csharp
// Tlačítko Create (2x)
<a asp-area="Admin" asp-controller="Collections" asp-action="Create" class="btn btn-primary">

// Tlačítko Edit
<a asp-area="Admin" asp-controller="Collections" asp-action="Edit" asp-route-id="@collection.Id">
```

#### Admin/Create.cshtml
```csharp
// Form action
<form asp-area="Admin" asp-controller="Collections" asp-action="Create" method="post" enctype="multipart/form-data">

// Cancel button
<a asp-area="Admin" asp-controller="Collections" asp-action="Index" class="btn btn-outline-secondary">
```

#### Admin/Edit.cshtml
```csharp
// Form action
<form asp-area="Admin" asp-controller="Collections" asp-action="Edit" method="post" enctype="multipart/form-data">
```

### 3. CSS & Design Updates
- Všechny admin views přepracovány na světlý design
- Bílé pozadí místo tmavého
- Konzistentní zlaté akcenty
- Responsive formuláře

### 4. Přidané inline styly pro debugging
```html
style="cursor: pointer; pointer-events: auto;"
```

## 🔧 Řešení problémů

### Problém: Tlačítko "Create New Collection" nereaguje

**Možné příčiny:**
1. Aplikace potřebuje restart (po přesunu controlleru)
2. CSS konflikt s Bootstrap
3. Nějaký JavaScript blokuje kliknutí

**Řešení:**

#### Krok 1: Restart aplikace
```bash
# Pokud je aplikace v Dockeru
docker-compose restart

# Nebo pokud běží přímo
dotnet build
dotnet run
```

#### Krok 2: Vyčistit cache prohlížeče
- Chrome/Edge: Ctrl+Shift+Del → Clear cache
- Firefox: Ctrl+Shift+Del → Clear cache
- Nebo zkuste Incognito/Private mode

#### Krok 3: Zkontrolovat routing manuálně
Zkuste přejít přímo na URL:
```
https://your-domain/Admin/Collections/Create
```

Pokud funguje přímý URL ale ne tlačítko:
- Problém je v JavaScript/CSS
- Zkontrolujte browser console (F12) na errory

#### Krok 4: Debug routing
Přidejte do `Program.cs` před `app.Run()`:
```csharp
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
```

### Problém: Dropdown kategorie nefunguje

**Možné příčiny:**
1. Bootstrap JavaScript není načten správně
2. Konflikt CSS
3. Form validation blokuje dropdown

**Řešení:**

#### Krok 1: Zkontrolovat Bootstrap JS
V `_Layout.cshtml` by měl být:
```html
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
```

#### Krok 2: Test dropdown přímo
Otevřete Create page a v browser console zadejte:
```javascript
document.querySelector('select[name="Category"]').value = "Paintings";
```

Pokud to funguje, dropdown HTML je v pořádku.

#### Krok 3: Zkontrolovat vygenerovaný HTML
Pravý klik na dropdown → Inspect → mělo by být:
```html
<select name="Category" id="Category" class="form-select">
    <option value="">-- Select Category --</option>
    <option value="Paintings">Paintings</option>
    <option value="Jewelry">Jewelry</option>
    <option value="Watches">Watches</option>
    <option value="Statues">Statues</option>
    <option value="Other">Other</option>
</select>
```

### Problém: Po odeslání formuláře chyba "stránka neexistuje"

**Možné příčiny:**
1. POST action není správně routovaný
2. ModelState validation selhala
3. Transaction rollback kvůli chybě

**Řešení:**

#### Krok 1: Zkontrolovat ModelState
Controller vrátí view s errory pokud validation selže.
Podívejte se na červené error hlášky ve formuláři.

#### Krok 2: Zkontrolovat povinná pole
```csharp
[Required] Title
[Required] Description  
[Required] Category
[Required] Minimálně 1 obrázek
```

#### Krok 3: Zkontrolovat logy
```bash
# V Dockeru
docker logs <container-name>

# Nebo v aplikaci
tail -f /var/log/your-app.log
```

#### Krok 4: Debug POST action
Přidejte breakpoint nebo logging do `Create` POST action:
```csharp
[HttpPost]
public async Task<IActionResult> Create(Collection model, List<IFormFile> images, IFormFile? audio)
{
    Console.WriteLine($"POST Create called: Title={model.Title}, Category={model.Category}");
    // ... rest of code
}
```

## 📋 Checklist pro testování

- [ ] Restart aplikace provedený
- [ ] Cache prohlížeče vyčištěna
- [ ] Přihlášen jako admin (`admin@localhost`)
- [ ] Na stránce `/Admin/Collections`
- [ ] Kliknutí na "Create New Collection" tlačítko
- [ ] Přechod na `/Admin/Collections/Create`
- [ ] Dropdown kategorie jde kliknout a vybrat
- [ ] Vyplnění formuláře (Title, Description, Category, obrázek)
- [ ] Submit formuláře
- [ ] Přesměrování na `/Admin/Collections` s novou kolekcí

## 🐛 Další možné problémy

### Pokud dropdown vypadá "disabled"
```css
/* Přidejte do site.css */
.form-select {
    pointer-events: auto !important;
    cursor: pointer !important;
    opacity: 1 !important;
}
```

### Pokud se formulář neodešle
Zkontrolujte AntiForgery token:
```html
<!-- Mělo by být ve formuláři -->
<input name="__RequestVerificationToken" type="hidden" value="..." />
```

### Pokud obrázky nejdou nahrát
Zkontrolujte oprávnění k `wwwroot/uploads/images/`:
```bash
chmod -R 755 wwwroot/uploads
```

## 📞 Support

Pokud problémy přetrvávají:
1. Zkontrolujte browser console (F12) na JavaScript errory
2. Zkontrolujte Network tab na failed requests
3. Podívejte se na application logy
4. Zkuste jiný prohlížeč

---

**Status:** Všechny opravy implementovány ✅  
**Vyžaduje:** Restart aplikace po změně controlleru
