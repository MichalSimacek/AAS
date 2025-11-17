# Implementation Plan - AAS Features v2.0

## 📋 Overview

Implementace nových funkcí:
1. AAS ikona s garancí autenticity
2. Komentáře na kolekce (registrovaní uživatelé)
3. Blog systém s rich text editorem
4. DeepL automatický překlad
5. "How to sell/buy" stránka
6. Landing page update

---

## 🗄️ Phase 1: Database Changes

### 1.1 Collection Model - AAS Verified Flag
```csharp
// Přidat do Collection.cs
public bool AASVerified { get; set; } = false;
```

**Migration:**
```csharp
migrationBuilder.AddColumn<bool>(
    name: "AASVerified",
    table: "Collections",
    type: "boolean",
    nullable: false,
    defaultValue: false);
```

### 1.2 Comment Model (NEW)
```csharp
public class Comment
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string UserId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Collection Collection { get; set; }
    public ApplicationUser User { get; set; }
}
```

### 1.3 BlogPost Model (NEW)
```csharp
public class BlogPost
{
    public int Id { get; set; }
    public string TitleCs { get; set; }
    public string TitleEn { get; set; }
    // ... další jazyky
    
    public string ContentCs { get; set; }
    public string ContentEn { get; set; }
    // ... další jazyky
    
    public string? FeaturedImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string AuthorId { get; set; }
    public bool Published { get; set; }
    
    public ApplicationUser Author { get; set; }
}
```

---

## 🔧 Phase 2: Backend Implementation

### 2.1 DeepL Integration Service
**Soubor:** `/Services/DeepLService.cs`

```csharp
public interface IDeepLService
{
    Task<string> TranslateAsync(string text, string targetLang, string sourceLang = "auto");
    Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text, string sourceLang);
}
```

**Endpoint:** https://api-free.deepl.com/v2/translate
**Vyžaduje:** DEEPL_API_KEY v .env

### 2.2 Comments Controller
**Soubor:** `/Controllers/CommentsController.cs`

Actions:
- `POST /api/comments/create` - Přidat komentář (auth required)
- `GET /api/comments/collection/{id}` - Načíst komentáře ke kolekci
- `PUT /api/comments/{id}` - Editovat komentář (owner nebo admin)
- `DELETE /api/comments/{id}` - Smazat komentář (owner nebo admin)

### 2.3 Blog Controller
**Soubor:** `/Controllers/BlogController.cs`

Public actions:
- `GET /blog` - Seznam článků
- `GET /blog/{id}` - Detail článku

Admin actions:
- `GET /admin/blog` - Správa článků
- `GET /admin/blog/create` - Formulář nového článku
- `POST /admin/blog/create` - Uložit článek
- `GET /admin/blog/edit/{id}` - Formulář editace
- `POST /admin/blog/edit/{id}` - Uložit změny
- `POST /admin/blog/delete/{id}` - Smazat článek
- `POST /admin/blog/translate/{id}` - Přeložit článek přes DeepL

### 2.4 Collections Controller Updates
Přidat checkbox `AASVerified` do Create/Edit formulářů.

---

## 🎨 Phase 3: Frontend Implementation

### 3.1 AAS Logo Ikona

**Připravit logo:**
1. Extrahovat logo z `/wwwroot/images/`
2. Vytvořit zmenšenou verzi (50x50px) s průhledným pozadím
3. Uložit jako `/wwwroot/images/aas-verified-badge.png`

**Collections Index View:**
```html
<!-- V thumbnail kartě -->
@if (Model.AASVerified)
{
    <div class="aas-verified-badge" title="@Localizer["AASVerifiedTooltip"]">
        <img src="~/images/aas-verified-badge.png" alt="AAS Verified" />
    </div>
}
```

**CSS:**
```css
.aas-verified-badge {
    position: absolute;
    top: 10px;
    left: 10px;
    z-index: 10;
}
```

### 3.2 Komentáře Komponenta

**Detail View - Comments Section:**
```html
<!-- Pod galerií a detaily -->
<div class="comments-section">
    <h3>@Localizer["Comments"]</h3>
    
    @if (User.Identity.IsAuthenticated)
    {
        <form id="commentForm">
            <textarea name="text" required></textarea>
            <button type="submit">@Localizer["AddComment"]</button>
        </form>
    }
    else
    {
        <p>@Localizer["LoginToComment"] <a href="/Identity/Account/Login">@Localizer["Login"]</a></p>
    }
    
    <div id="commentsList">
        @foreach (var comment in Model.Comments)
        {
            <div class="comment">
                <strong>@comment.User.UserName</strong>
                <span class="date">@comment.CreatedAt.ToString("dd.MM.yyyy HH:mm")</span>
                <p>@comment.Text</p>
                
                @if (User.Identity.Name == comment.User.UserName || User.IsInRole("Admin"))
                {
                    <button onclick="deleteComment(@comment.Id)">@Localizer["Delete"]</button>
                    <button onclick="editComment(@comment.Id)">@Localizer["Edit"]</button>
                }
            </div>
        }
    </div>
</div>
```

**JavaScript:**
```javascript
// /wwwroot/js/comments.js
async function addComment(collectionId, text) { ... }
async function editComment(id) { ... }
async function deleteComment(id) { ... }
```

### 3.3 Blog Admin Panel

**Rich Text Editor:** TinyMCE (CDN)

**Admin Blog Index:**
- Tabulka článků
- Tlačítka: Create, Edit, Delete, Translate

**Admin Blog Create/Edit:**
- Tabs pro jazyky (CS, EN, DE, ...)
- TinyMCE pro Content
- Upload Featured Image
- Tlačítko "Translate with DeepL"
- Publish checkbox

### 3.4 Blog Public Pages

**/Views/Blog/Index.cshtml:**
- Grid článků s titulním obrázkem
- Excerpt (prvních 200 znaků)
- Datum publikace
- "Read more" odkaz

**/Views/Blog/Detail.cshtml:**
- Titulní obrázek
- Datum publikace
- HTML content (z TinyMCE)

### 3.5 How to Sell/Buy Page

**/Views/HowTo/Index.cshtml:**

Sekce:
1. **Úvod** - Co je Aristocratic Artwork Sale
2. **Jak koupit:**
   - Procházení kolekcí
   - Inquiry formulář
   - Komunikace s prodejcem
   - Transakce
3. **Jak prodat:**
   - Kontakt na info@aristocraticartworksale.com
   - Posílání fotek a detailů
   - Vytvoření inzerce
   - Provize
4. **AAS Verified ikona:**
   - Co znamená
   - Kdo ručí za pravost
   - Rozdíl mezi verified/unverified

### 3.6 Landing Page Update

**Home/Index.cshtml:**

Pod hero sekci přidat:
```html
<section class="sell-with-us">
    <h2>@Localizer["BuyOrSellThroughUs"]</h2>
    <p>@Localizer["BuyOrSellDescription"]</p>
    <a href="/HowTo" class="btn btn-primary">@Localizer["LearnMore"]</a>
</section>
```

---

## 🌐 Phase 4: Localization

### 4.1 Nové klíče v Resources

**SharedResources.*.resx:**

```
AASVerifiedTooltip = "Autenticita předmětu garantována společností AAS"
Comments = "Komentáře"
AddComment = "Přidat komentář"
LoginToComment = "Pro přidání komentáře se přihlaste"
Edit = "Upravit"
Delete = "Smazat"
BlogPosts = "Blog články"
CreatePost = "Vytvořit článek"
FeaturedImage = "Titulní obrázek"
PublishDate = "Datum publikace"
ReadMore = "Číst více"
TranslateWithDeepL = "Přeložit pomocí DeepL"
BuyOrSellThroughUs = "Kupujte a prodávejte s námi"
BuyOrSellDescription = "Objevte jak bezpečně koupit nebo prodat luxusní předměty"
LearnMore = "Zjistit více"
HowToBuy = "Jak koupit"
HowToSell = "Jak prodat"
```

Pro všechny jazyky: CS, EN, DE, ES, FR, HI, JA, PT, RU, ZH

---

## 🔐 Phase 5: Security & Authorization

### 5.1 Comments Authorization
- Pouze přihlášení uživatelé mohou přidávat komentáře
- Owner nebo Admin může editovat/mazat

### 5.2 Blog Authorization
- Veřejné čtení
- Admin-only pro CRUD operace

### 5.3 Rate Limiting
Přidat rate limiting pro:
- Komentáře (max 5/min)
- DeepL API (aby neprošláply limity)

---

## 🧪 Phase 6: Testing Checklist

### 6.1 AAS Ikona
- [ ] Ikona se zobrazí pouze u verified kolekcí
- [ ] Tooltip funguje při hover
- [ ] Responsive design (mobile/desktop)
- [ ] Ikona neruší náhled

### 6.2 Komentáře
- [ ] Přihlášený uživatel může přidat komentář
- [ ] Nepřihlášený vidí výzvu k přihlášení
- [ ] Vlastník může editovat/mazat svůj komentář
- [ ] Admin může mazat jakýkoliv komentář
- [ ] Komentáře se zobrazují chronologicky
- [ ] XSS protection (sanitize HTML)

### 6.3 Blog
- [ ] Admin může vytvořit článek
- [ ] Rich text editor funguje
- [ ] Upload obrázků funguje
- [ ] Publish/unpublish funguje
- [ ] Veřejná stránka zobrazuje pouze published
- [ ] Detail článku zobrazuje správný jazyk
- [ ] DeepL překlad funguje

### 6.4 DeepL Integration
- [ ] Manuální překlad tlačítkem funguje
- [ ] Auto překlad při změně jazyka funguje
- [ ] Error handling při API limitu
- [ ] Translations ukládají do DB

### 6.5 How to Sell/Buy
- [ ] Stránka je přístupná
- [ ] Všechny sekce jsou přeložené
- [ ] Odkazy fungují
- [ ] Responsive design

### 6.6 Landing Page
- [ ] Nová sekce se zobrazuje
- [ ] Odkaz vede na /HowTo
- [ ] Přeloženo do všech jazyků

---

## 📦 Deployment Steps

### 1. Database Migration
```bash
cd /AAS
sudo docker compose -f docker-compose.prod.yml exec web dotnet ef migrations add AddCommentsAndBlog
sudo docker compose -f docker-compose.prod.yml exec web dotnet ef database update
```

### 2. Environment Variables
```bash
# Přidat do .env
DEEPL_API_KEY=<your_key>
```

### 3. Build & Deploy
```bash
cd /AAS
git pull origin main
sudo docker compose -f docker-compose.prod.yml build --no-cache web
sudo docker compose -f docker-compose.prod.yml up -d --force-recreate web
```

### 4. Verify
- Zkontrolovat logy
- Otestovat všechny nové funkce
- Ověřit překlady

---

## 📊 Estimated Timeline

- **Phase 1 (Database):** 2 hodiny
- **Phase 2 (Backend):** 6 hodin
- **Phase 3 (Frontend):** 8 hodin
- **Phase 4 (Localization):** 3 hodiny
- **Phase 5 (Security):** 2 hodiny
- **Phase 6 (Testing):** 4 hodiny

**Total:** ~25 hodin práce

---

## 🎯 Priority Order

1. ✅ Email mailing (HOTOVO)
2. 🔄 AAS Ikona (jednoduchá, rychlá)
3. 🔄 "How to Sell/Buy" stránka (statická, bez DB)
4. 🔄 Landing page update
5. 🔄 Komentáře (střední složitost)
6. 🔄 Blog (nejvíce práce)
7. 🔄 DeepL integrace (závislá na blog)

---

## 💡 Notes

- TinyMCE je zdarma pro základní použití
- DeepL Free tier: 500,000 znaků/měsíc
- Všechny změny budou commitnuty jako "STABLE v2.0"
- Po dokončení bude vytvořen migration guide
