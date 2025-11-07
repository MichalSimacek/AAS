# Razor Syntax Fixes

## Opravené chyby

### 1. Chybějící ViewData direktivy
**Soubory:** `About/Index.cshtml`, `Contacts/Index.cshtml`

**Problém:** Chyběla inicializace ViewData na začátku souborů.

**Oprava:**
```csharp
@{
    ViewData["Title"] = "About Us";  // nebo "Contact Us"
}
```

### 2. Escapování @ symbolu
**Soubor:** `Contacts/Index.cshtml`

**Problém:** Symbol `@` v HTML byl interpretován jako začátek Razor kódu.

**Původní:**
```html
<span>@</span>
<strong>aristocratic-artwork-sell@proton.me</strong>
```

**Opraveno:**
```html
<span>&#64;</span>
<strong>aristocratic-artwork-sell&#64;proton.me</strong>
```

### 3. Lambda výrazy s => operátorem
**Soubory:** `Collections/Index.cshtml`, `Collections/Detail.cshtml`, `Admin/Edit.cshtml`

**Problém:** Lambda výraz `i=>i.SortOrder` bez mezer způsobuje, že Razor parser interpretuje `>` jako HTML tag.

**Původní:**
```csharp
var first = c.Images.OrderBy(i=>i.SortOrder).FirstOrDefault();
```

**Opraveno:**
```csharp
var first = c.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
```

## Testování

Pro ověření, že všechny opravy fungují:

1. Otevřete projekt v Visual Studio nebo VS Code
2. Spusťte build:
   ```
   dotnet build
   ```
3. Pokud build projde bez chyb, aplikace by měla fungovat správně

## Souhrn změněných souborů

- ✅ `/app/src/AAS.Web/Views/About/Index.cshtml`
- ✅ `/app/src/AAS.Web/Views/Contacts/Index.cshtml`
- ✅ `/app/src/AAS.Web/Views/Collections/Index.cshtml`
- ✅ `/app/src/AAS.Web/Views/Collections/Detail.cshtml`
- ✅ `/app/src/AAS.Web/Views/Admin/Edit.cshtml`

## Důležité poznámky pro budoucí vývoj

1. **Vždy používejte mezery v lambda výrazech:** `i => i.Property` místo `i=>i.Property`
2. **Escapujte @ symbol:** Použijte `&#64;` nebo `@@` v místech, kde chcete zobrazit `@` jako text
3. **Vždy přidejte ViewData:** Na začátku každého view souboru by měl být @{} blok s ViewData["Title"]
4. **Testujte build často:** Po každé větší změně v Razor views
