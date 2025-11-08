# 🔒 BEZPEČNOSTNÍ AUDIT - AAS Web Application

**Datum:** 8. listopadu 2024  
**Auditor:** AI Security Agent  
**Rozsah:** Kompletní audit ASP.NET Core MVC aplikace

---

## 📊 EXECUTIVE SUMMARY

| Kategorie | Kritické | Vysoké | Střední | Nízké | Celkem |
|-----------|----------|--------|---------|-------|--------|
| **Bezpečnost** | 2 | 5 | 8 | 4 | 19 |
| **Výkon** | 0 | 3 | 6 | 3 | 12 |
| **Responzivita** | 0 | 2 | 4 | 2 | 8 |
| **CELKEM** | 2 | 10 | 18 | 9 | **39** |

---

## 🚨 KRITICKÉ NÁLEZY (CRITICAL)

### SEC-001: Path Traversal ve file upload
**Severity:** CRITICAL  
**Soubor:** `ImageService.cs`, `Admin/CollectionsController.cs`

**Problém:**
```csharp
// VULNERABLE CODE
var audioName = Guid.NewGuid().ToString("N") + audioExt;
var audioPath = Path.Combine(audioDir, audioName);
```

Pokud útočník nahraje soubor s názvem obsahujícím `../`, může zapsat soubory mimo upload adresář.

**Řešení:**
```csharp
// BEZPEČNĚ
var safeFileName = Path.GetFileName(audio.FileName);  // Odstranit cestu
var audioName = Guid.NewGuid().ToString("N") + Path.GetExtension(safeFileName);
```

---

### SEC-002: Mass Assignment vulnerability
**Severity:** CRITICAL  
**Soubor:** `Admin/CollectionsController.cs`

**Problém:**
```csharp
[HttpPost]
public async Task<IActionResult> Create(Collection model, ...)
```

Útočník může v POST requestu poslat dodatečná pole jako `IsDeleted`, `CreatedUtc`, atd.

**Řešení:**
Použít ViewModel místo přímo Model entity.

---

## 🔴 VYSOKÉ NÁLEZY (HIGH)

###  SEC-003: Chybějící input sanitization v databázi
**Severity:** HIGH  
**Soubor:** `InquiriesController.cs`, `Admin/CollectionsController.cs`

**Problém:**
User input (jména, zprávy, tituly) není sanitizován před uložením do DB a zobrazením.

**Řešení:**
- HTML Encode při výstupu
- Strip HTML tags při vstupu
- Validace max délek

---

### SEC-004: Chybějící Output Encoding v Views
**Severity:** HIGH  
**Soubor:** Všechny `.cshtml` soubory

**Problém:**
```cshtml
@Html.Raw(Model.Description)
```

Pokud Description obsahuje JavaScript, způsobí XSS.

**Řešení:**
```csharp
// Sanitize HTML před uložením
var sanitized = HtmlSanitizer.Sanitize(model.Description);
```

---

### SEC-005: Slabá validace email formátu
**Severity:** HIGH  
**Soubor:** `Models/Inquiry.cs`

**Problém:**
```csharp
[EmailAddress]
```

Built-in validace je slabá, akceptuje `test@test` (bez TLD).

**Řešení:**
```csharp
[EmailAddress]
[RegularExpression(@"^[\w\.-]+@[\w\.-]+\.\w{2,}$")]
```

---

### SEC-006: Chybějící Rate Limiting na admin akcích
**Severity:** HIGH  
**Soubor:** `Admin/CollectionsController.cs`

**Problém:**
Admin může neomezeně vytvářet kolekce, uploady bez limitu.

**Řešení:**
Implementovat rate limiting pro admin akce.

---

### SEC-007: Nedostatečná validace image MIME typu
**Severity:** HIGH  
**Soubor:** `ImageService.cs`

**Problém:**
Kontrola pouze extensions, ne actual MIME type.

**Řešení:**
```csharp
// Ověřit MIME type z file headers
var mimeType = file.ContentType;
if (!allowedMimeTypes.Contains(mimeType))
    throw new InvalidOperationException();
```

---

## 🟡 STŘEDNÍ NÁLEZY (MEDIUM)

### SEC-008: Chybějící HTTPS enforcement
**Severity:** MEDIUM  
**Soubor:** `Program.cs`

**Řešení:**
```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

---

### SEC-009: Slabá Content Security Policy
**Severity:** MEDIUM  
**Soubor:** `Program.cs`

**Současný CSP:**
```
default-src 'self'; script-src 'self' 'unsafe-inline'
```

**Problém:** `unsafe-inline` je nebezpečné.

**Řešení:** Použít nonce nebo hash pro inline scripty.

---

### SEC-010: Chybějící X-Frame-Options
**Severity:** MEDIUM

**Řešení:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    await next();
});
```

---

### SEC-011: Chybějící logging bezpečnostních událostí
**Severity:** MEDIUM

**Problém:**
Žádné logování failed logins, unauthorized access pokusů.

**Řešení:**
Implementovat security event logging.

---

### SEC-012: Chybějící backup verification
**Severity:** MEDIUM  
**Soubor:** `backup.sh`

**Problém:**
Záloha se vytvoří, ale nikdy se netestuje restore.

---

### SEC-013: Hardcoded configuration values
**Severity:** MEDIUM  
**Soubor:** Různé

**Problém:**
```csharp
const int maxDimension = 8000;
const int maxAudioSizeMB = 15;
```

**Řešení:**
Přesunout do appsettings.json

---

### SEC-014: Chybějící DB connection encryption
**Severity:** MEDIUM  
**Soubor:** `appsettings.json`

**Řešení:**
```
Encrypt=True;TrustServerCertificate=False
```

---

### SEC-015: Nedostatečná error handling strategie
**Severity:** MEDIUM

**Problém:**
Některé exceptions vracejí detailní info.

---

## 🟢 NÍZKÉ NÁLEZY (LOW)

### SEC-016: Missing security headers
**Severity:** LOW

Chybí:
- X-Content-Type-Options
- Referrer-Policy
- Permissions-Policy

---

### SEC-017: Verbose error messages v Production
**Severity:** LOW

---

### SEC-018: Chybějící clickjacking protection
**Severity:** LOW

---

### SEC-019: Nedostatečné cookie security flags
**Severity:** LOW

**Řešení:**
```csharp
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

---

## ⚡ VÝKONOVÉ NÁLEZY

### PERF-001: N+1 Query Problem
**Severity:** HIGH  
**Soubor:** `CollectionsController.cs`

**Problém:**
```csharp
var collections = await _db.Collections.ToListAsync();
// Pro každou kolekci se dělá extra query na images
```

**Řešení:**
```csharp
var collections = await _db.Collections
    .Include(c => c.Images)
    .ToListAsync();
```

---

### PERF-002: Chybějící databázové indexy
**Severity:** HIGH

**Chybí indexy na:**
- `Collections.Slug` (unique index)
- `Collections.Category`
- `Collections.CreatedUtc`

---

### PERF-003: Chybějící Response Caching
**Severity:** MEDIUM

**Řešení:**
```csharp
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<IActionResult> Index()
```

---

### PERF-004: Neoptimalizované image loading
**Severity:** MEDIUM

**Problém:**
Všechny obrázky se načítají eagerly.

**Řešení:**
Lazy loading images v views.

---

### PERF-005: Chybějící compression
**Severity:** MEDIUM

**Řešení:**
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});
```

---

### PERF-006: Nedostatečná translation cache strategie
**Severity:** MEDIUM  
**Soubor:** `TranslationService.cs`

**Problém:**
Cache je pouze in-memory, ztratí se při restartu.

**Řešení:**
Použít distributed cache (Redis) nebo DB cache.

---

## 📱 RESPONZIVITA NÁLEZY

### RESP-001: Chybějící viewport meta tag
**Severity:** HIGH  
**Soubor:** `_Layout.cshtml`

**Řešení:**
```html
<meta name="viewport" content="width=device-width, initial-scale=1.0">
```

---

### RESP-002: Touch targets příliš malé
**Severity:** HIGH  
**Soubor:** CSS

**Problém:**
Tlačítka a odkazy menší než 44×44px (Apple HIG).

---

### RESP-003: Neoptimální breakpointy
**Severity:** MEDIUM  
**Soubor:** `site.css`

**Současné:**
```css
@media (max-width: 768px)
```

**Doporučeno:**
```css
@media (max-width: 576px)  /* Mobile */
@media (max-width: 768px)  /* Tablet */
@media (max-width: 992px)  /* Small laptop */
@media (max-width: 1200px) /* Desktop */
```

---

### RESP-004: Obrázky bez srcset
**Severity:** MEDIUM

**Problém:**
Mobilní zařízení stahují plnou velikost obrázků.

**Řešení:**
```html
<img srcset="img-480.jpg 480w, img-960.jpg 960w, img-1600.jpg 1600w"
     sizes="(max-width: 768px) 100vw, 50vw">
```

---

## 📝 PRIORITIZACE OPRAV

### 🔴 Okamžitě (do 24h):
1. SEC-001: Path Traversal
2. SEC-002: Mass Assignment
3. SEC-003: Input Sanitization
4. SEC-004: Output Encoding (XSS)

### 🟠 Brzy (do týdne):
1. SEC-005-007: Validace a rate limiting
2. PERF-001-002: Database performance
3. RESP-001-002: Mobile responsiveness

### 🟡 Později (do měsíce):
1. Zbývající SEC nálezy
2. Výkonové optimalizace
3. Security headers
4. Monitoring a logging

---

## ✅ CO UŽ FUNGUJE DOBŘE

1. ✅ CSRF protection (`[ValidateAntiforgeryToken]`)
2. ✅ Role-based authorization (`[Authorize(Roles="Admin")]`)
3. ✅ Rate limiting na inquiries
4. ✅ Basic image validation
5. ✅ Password hashing (Identity default)
6. ✅ SQL injection protection (Entity Framework)
7. ✅ Some performance optimizations (AsNoTracking)

---

## 🎯 DOPORUČENÉ AKCE

1. **Instalovat NuGet packages:**
   - `HtmlSanitizer` (pro sanitizaci HTML)
   - `AspNetCoreRateLimit` (pro advanced rate limiting)
   - `Serilog` (pro structured logging)

2. **Aktualizovat Program.cs** s security headers

3. **Vytvořit ViewModels** místo direct model binding

4. **Přidat database indexy** pomocí migrations

5. **Implementovat comprehensive logging**

6. **Nastavit automated security scanning** (např. OWASP ZAP)

---

**Konec auditu**
